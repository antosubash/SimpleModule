---
outline: deep
---

# Events

Modules communicate without direct references by publishing events. SimpleModule builds on **[Wolverine](https://wolverinefx.net/)** for in-process messaging: handlers are discovered by convention and invoked through `IMessageBus`.

## Core Concepts

### IEvent

`IEvent` is a marker interface. Any record or class implementing it is treated as a domain event by the framework (audit capture, domain-event dispatch from `AuditableAggregateRoot`, etc.). Events are typically defined in a module's **Contracts** project so other modules can reference them without depending on the implementation.

```csharp
using SimpleModule.Core.Events;

public sealed record OrderCreatedEvent(OrderId OrderId, UserId UserId, decimal Total) : IEvent;
```

### Publishing with IMessageBus

Inject Wolverine's `IMessageBus` and call `PublishAsync`:

```csharp
using Wolverine;

public sealed partial class OrderService(
    OrdersDbContext db,
    IMessageBus bus,
    ILogger<OrderService> logger
) : IOrderContracts
{
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order { UserId = request.UserId, Total = request.Total };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await bus.PublishAsync(new OrderCreatedEvent(order.Id, order.UserId, order.Total));

        return order;
    }
}
```

`IMessageBus` is registered as scoped by `AddSimpleModuleInfrastructure()` — no per-module wiring needed.

::: tip Breaking factory cycles
If two services form a cycle through the bus (for example, a settings service whose decorator also needs the bus), inject `Lazy<IMessageBus>` instead. The framework registers it out of the box.
:::

### Writing a Handler

Wolverine discovers handlers by **naming convention**: a public class whose type or name ends with `Handler` / `Consumer`, with a method named `Handle` / `Consume` / `HandleAsync` that takes the event as its first parameter. No interface, no DI registration.

```csharp
public sealed class OrderCreatedNotificationHandler(INotificationService notifications)
{
    public Task Handle(OrderCreatedEvent evt, CancellationToken ct) =>
        notifications.SendAsync(evt.UserId, $"Order {evt.OrderId} confirmed", ct);
}
```

Handlers resolve through the request scope, so injected services (DbContext, loggers, contracts) behave exactly as they would inside an endpoint.

## Dispatching Domain Events from Aggregates

Entities that derive from `AuditableAggregateRoot` (or implement `IHasDomainEvents`) can queue events that are flushed via `IMessageBus` after `SaveChangesAsync()` succeeds. This keeps write logic transactional: events only fire if the save commits.

```csharp
public sealed class Order : AuditableAggregateRoot<OrderId>
{
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }

    public void Confirm()
    {
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, Total));
    }
}
```

The `DomainEventInterceptor` (registered by the hosting layer) picks up queued events after a successful save and publishes them through the bus.

## Delivery Semantics

Wolverine routes `PublishAsync` to **every matching handler** in the process:

- **In-process only.** The framework configures Wolverine with no external transports and no durable outbox — events are not persisted and are not retried across process restarts.
- **Handler isolation.** Each handler runs in its own dispatch. A failing handler does not stop dispatch to the others.
- **Exceptions surface.** By default, handler exceptions are logged and rethrown once all handlers have been attempted. If you need finer control, configure Wolverine policies in `builder.Host.UseWolverine(opts => ...)`.
- **Audit capture.** The AuditLogs module wraps `IMessageBus` with `AuditingMessageBus`, which records an audit entry for every published `IEvent`. Audit failures are swallowed and logged — they never break the primary operation.

::: warning Not a durable queue
Wolverine is running in-memory here. For work that must survive a restart, use the [Background Jobs](/guide/background-jobs) module instead of relying on events.
:::

## Handler Best Practices

### Keep Handlers Focused

A handler should do one thing. If `OrderCreatedEvent` needs to send an email, update a search index, and invalidate caches, write three handlers. Wolverine invokes them independently.

### Be Idempotent

An event may be replayed (retry logic, re-run of a background job). Handlers should tolerate seeing the same event twice — check for existing state before writing.

### Don't Throw for Non-Critical Work

Audit logging, metrics, cache invalidation, and similar cross-cutting concerns should catch their own exceptions. Reserve rethrown exceptions for failures the caller actually needs to know about.

```csharp
public sealed class OrderMetricsHandler(IMetrics metrics, ILogger<OrderMetricsHandler> logger)
{
    public Task Handle(OrderCreatedEvent evt, CancellationToken ct)
    {
        try
        {
            metrics.Increment("orders.created", tags: new { evt.UserId });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record order metrics");
        }
#pragma warning restore CA1031
        return Task.CompletedTask;
    }
}
```

### Offload Long-Running Work

Handlers run inline with the publishing scope. For anything expensive (external HTTP, PDF rendering, batch writes), enqueue a background job instead of blocking the caller.

## Testing Events

### Unit-Testing a Handler

Instantiate the handler directly. No DI container is required.

```csharp
[Fact]
public async Task OrderCreatedNotificationHandler_sends_confirmation()
{
    var notifications = Substitute.For<INotificationService>();
    var handler = new OrderCreatedNotificationHandler(notifications);

    await handler.Handle(
        new OrderCreatedEvent(OrderId.From(1), UserId.From(42), 99.99m),
        CancellationToken.None
    );

    await notifications.Received().SendAsync(UserId.From(42), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
```

### Verifying Publishes in a Service Test

In service-level tests, substitute `IMessageBus` and assert on the recorded calls:

```csharp
[Fact]
public async Task CreateOrder_publishes_order_created_event()
{
    var bus = Substitute.For<IMessageBus>();
    var service = new OrderService(db, bus, NullLogger<OrderService>.Instance);

    var order = await service.CreateOrderAsync(new CreateOrderRequest(UserId.From(42), 99.99m));

    await bus.Received().PublishAsync(Arg.Is<OrderCreatedEvent>(e => e.OrderId == order.Id));
}
```

## Next Steps

- [Permissions](/guide/permissions) — claims-based authorization for endpoints
- [Database](/guide/database) — persistence patterns commonly paired with events
- [Unit Tests](/testing/unit-tests) — how to test event handlers and service-level publishing
