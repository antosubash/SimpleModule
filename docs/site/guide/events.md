---
outline: deep
---

# Event Bus

The event bus enables decoupled communication between modules. Instead of modules referencing each other directly, they publish events that any module can subscribe to. This keeps modules independent while allowing cross-cutting behavior like audit logging, notifications, or cache invalidation.

## Core Concepts

### IEvent

`IEvent` is a marker interface. Any record or class implementing it can be published through the event bus:

```csharp
using SimpleModule.Core.Events;

public sealed record OrderCreatedEvent(OrderId OrderId, UserId UserId, decimal Total) : IEvent;
```

Events are typically defined in a module's **Contracts** project so other modules can reference them without depending on the implementation.

### IEventBus

`IEventBus` exposes a single method for publishing events to all registered handlers:

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent;
}
```

Inject `IEventBus` into any service and call `PublishAsync`:

```csharp
public sealed partial class OrderService(
    OrdersDbContext db,
    IEventBus eventBus,
    ILogger<OrderService> logger
) : IOrderContracts
{
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        // ... create the order ...

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await eventBus.PublishAsync(
            new OrderCreatedEvent(order.Id, order.UserId, order.Total)
        );

        return order;
    }
}
```

### IEventHandler\<T\>

Implement `IEventHandler<T>` to react to a specific event type. Register handlers in DI and they are automatically discovered by the event bus:

```csharp
public sealed class OrderCreatedNotificationHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(
        OrderCreatedEvent @event,
        CancellationToken cancellationToken
    )
    {
        // Send notification, update cache, etc.
    }
}
```

Register in your module's `ConfigureServices`:

```csharp
services.AddScoped<IEventHandler<OrderCreatedEvent>, OrderCreatedNotificationHandler>();
```

## Partial Success Semantics

The event bus guarantees that **all handlers execute even if some fail**. This is the most important design decision in the event system.

### How It Works

1. Handlers execute **sequentially** in registration order
2. If a handler throws, the exception is **caught and logged**
3. Execution **continues** to the next handler
4. After all handlers complete, any collected exceptions are thrown as a single `AggregateException`

```
Handler A  ──→ ✅ Success (side effects preserved)
Handler B  ──→ ❌ Throws (exception caught and logged)
Handler C  ──→ ✅ Success (still executes despite B's failure)

Result: AggregateException containing B's exception
```

::: warning
Side effects from successful handlers are **preserved** even when the `AggregateException` is thrown. Design your error handling accordingly.
:::

### Handling AggregateException

The caller is responsible for handling the aggregate exception. Inspect `InnerExceptions` to see which handlers failed:

```csharp
try
{
    await eventBus.PublishAsync(new OrderCreatedEvent(orderId, userId, total));
}
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
    {
        logger.LogError(inner, "Handler failed for OrderCreatedEvent");
    }
}
```

## Handler Best Practices

### Be Stateless

Handlers may be called concurrently in future versions. Avoid mutable state:

```csharp
// Good: stateless, uses injected services
public sealed class AuditHandler(IAuditContext audit) : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        await audit.LogAsync("Order created", @event.OrderId.ToString());
    }
}
```

### Be Independent

Do not rely on side effects from other handlers. They may execute in any order or be skipped in future versions.

### Be Idempotent

The same event may be reprocessed in retry scenarios. Design handlers to handle duplicate calls gracefully.

### Don't Throw for Expected Failures

For non-critical work like audit logging, catch exceptions inside the handler rather than letting them propagate:

```csharp
public sealed class AuditLogEventHandler(
    IAuditContext audit,
    ILogger<AuditLogEventHandler> logger
) : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(
        OrderCreatedEvent @event,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await audit.LogAsync("Order created", @event.OrderId.ToString());
        }
        catch (Exception ex)
        {
            // Don't throw: audit logging must never disrupt the primary operation
            logger.LogError(ex, "Failed to log event");
        }
    }
}
```

### Avoid Long-Running Work

For expensive operations, queue work for a background service instead of blocking the event bus:

```csharp
public sealed class EmailHandler(EmailChannel channel) : IEventHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        // Queue for background processing instead of sending synchronously
        return channel.EnqueueAsync(new OrderConfirmationEmail(@event.OrderId));
    }
}
```

## Testing Events

### Basic Handler Test

```csharp
[Fact]
public async Task Handler_processes_event()
{
    var handler = new OrderCreatedNotificationHandler();
    var @event = new OrderCreatedEvent(OrderId.From(1), UserId.From(1), 99.99m);

    await handler.HandleAsync(@event, CancellationToken.None);

    // Assert side effects
}
```

### Testing Partial Failure

Verify that successful handlers complete their work even when other handlers fail:

```csharp
[Fact]
public async Task Successful_handlers_complete_when_others_fail()
{
    var services = new ServiceCollection();
    services.AddScoped<IEventHandler<TestEvent>, SuccessfulHandler>();
    services.AddScoped<IEventHandler<TestEvent>, FailingHandler>();
    services.AddScoped<IEventHandler<TestEvent>, AnotherSuccessfulHandler>();

    var provider = services.BuildServiceProvider();
    var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

    var ex = await Assert.ThrowsAsync<AggregateException>(
        () => bus.PublishAsync(new TestEvent("value"))
    );

    ex.InnerExceptions.Should().HaveCount(1);
    // Verify successful handlers completed their work
}
```

### Testing Handler Execution Order

Handlers run in registration order. You can verify this:

```csharp
[Fact]
public async Task Handlers_execute_in_registration_order()
{
    var order = new List<string>();

    var services = new ServiceCollection();
    services.AddScoped<IEventHandler<TestEvent>>(_ => new OrderTrackingHandler("A", order));
    services.AddScoped<IEventHandler<TestEvent>>(_ => new OrderTrackingHandler("B", order));

    var provider = services.BuildServiceProvider();
    var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

    await bus.PublishAsync(new TestEvent("value"));

    order.Should().ContainInOrder("A", "B");
}
```

## EventBus Internals

The `EventBus` implementation resolves all `IEventHandler<T>` instances from `IServiceProvider` and iterates through them:

```csharp
public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
    where T : IEvent
{
    var handlers = serviceProvider.GetServices<IEventHandler<T>>();
    List<Exception>? exceptions = null;

    foreach (var handler in handlers)
    {
        try
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            LogHandlerFailed(logger, handler.GetType().Name, typeof(T).Name, ex);
            exceptions ??= [];
            exceptions.Add(ex);
        }
    }

    if (exceptions is { Count: > 0 })
    {
        throw new AggregateException(
            $"One or more event handlers for {typeof(T).Name} failed.",
            exceptions
        );
    }
}
```

Key implementation details:

- Handlers are resolved via `GetServices<IEventHandler<T>>()`, so registration order matters
- Failed handlers are logged with structured logging (`[LoggerMessage]` source generator)
- The `CancellationToken` is passed to every handler, allowing cooperative cancellation
- The `EventBus` is registered as scoped, so handlers share the same DI scope as the request

## Next Steps

- [Permissions](/guide/permissions) -- claims-based authorization for endpoints
- [Database](/guide/database) -- persistence patterns commonly paired with events
- [Unit Tests](/testing/unit-tests) -- how to test event handlers and partial failure scenarios
