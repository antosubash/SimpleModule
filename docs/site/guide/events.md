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

public sealed record CustomerCreatedEvent(CustomerId CustomerId, UserId CreatedBy, string Email) : IEvent;
```

### Publishing with IMessageBus

Inject Wolverine's `IMessageBus` and call `PublishAsync`:

```csharp
using Wolverine;

public sealed partial class CustomerService(
    CustomersDbContext db,
    IMessageBus bus,
    ILogger<CustomerService> logger
) : ICustomerContracts
{
    public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var customer = new Customer { Name = request.Name, Email = request.Email };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        await bus.PublishAsync(new CustomerCreatedEvent(customer.Id, request.CreatedBy, customer.Email));

        return customer;
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
public sealed class CustomerCreatedNotificationHandler(INotificationService notifications)
{
    public Task Handle(CustomerCreatedEvent evt, CancellationToken ct) =>
        notifications.SendAsync(evt.CreatedBy, $"Customer {evt.CustomerId} created", ct);
}
```

Handlers resolve through the request scope, so injected services (DbContext, loggers, contracts) behave exactly as they would inside an endpoint.

## Dispatching Domain Events from Aggregates

Entities that derive from `AuditableAggregateRoot` (or implement `IHasDomainEvents`) can queue events that are flushed via `IMessageBus` after `SaveChangesAsync()` succeeds. This keeps write logic transactional: events only fire if the save commits.

```csharp
public sealed class Customer : AuditableAggregateRoot<CustomerId>
{
    public string Name { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; }

    public void Activate()
    {
        Status = CustomerStatus.Active;
        AddDomainEvent(new CustomerActivatedEvent(Id, Name));
    }
}
```

The `DomainEventInterceptor` (registered by the hosting layer) picks up queued events after a successful save and publishes them through the bus.

## Delivery Semantics

Wolverine routes `PublishAsync` to **every matching handler** in the process:

- **Durable inbox/outbox.** The framework persists envelopes to the configured database (PostgreSQL, SQL Server, or SQLite) via Wolverine's EF Core integration — `PersistMessagesWithPostgresql/SqlServer/Sqlite` plus `UseDurableInboxOnAllListeners()`. Events queued by `SaveChangesAndFlushMessagesAsync` commit atomically with the EF write, so handlers never run for a transaction that rolled back.
- **Restart-safe.** Envelopes in flight when the process dies are picked up by the next instance and dispatched.
- **Handler isolation.** Each handler runs in its own dispatch. A failing handler does not stop dispatch to the others.
- **Exceptions surface.** By default, handler exceptions are logged and rethrown once all handlers have been attempted. If you need finer control, configure Wolverine policies in `builder.Host.UseWolverine(opts => ...)`.
- **Audit capture.** The AuditLogs module wraps `IMessageBus` with `AuditingMessageBus`, which records an audit entry for every published `IEvent`. Audit failures are swallowed and logged — they never break the primary operation.

::: tip Long-running work
Handlers still run inline with the publishing scope. For anything expensive (PDF rendering, external HTTP, batch writes) hand off to the [Background Jobs](/guide/background-jobs) module from inside the handler rather than blocking the dispatch.
:::

## Handler Best Practices

### Keep Handlers Focused

A handler should do one thing. If `CustomerCreatedEvent` needs to send a welcome email, update a search index, and invalidate caches, write three handlers. Wolverine invokes them independently.

### Be Idempotent

An event may be replayed (retry logic, re-run of a background job). Handlers should tolerate seeing the same event twice — check for existing state before writing.

### Don't Throw for Non-Critical Work

Audit logging, metrics, cache invalidation, and similar cross-cutting concerns should catch their own exceptions. Reserve rethrown exceptions for failures the caller actually needs to know about.

```csharp
public sealed class CustomerMetricsHandler(IMetrics metrics, ILogger<CustomerMetricsHandler> logger)
{
    public Task Handle(CustomerCreatedEvent evt, CancellationToken ct)
    {
        try
        {
            metrics.Increment("customers.created", tags: new { evt.CreatedBy });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record customer metrics");
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
public async Task CustomerCreatedNotificationHandler_sends_confirmation()
{
    var notifications = Substitute.For<INotificationService>();
    var handler = new CustomerCreatedNotificationHandler(notifications);

    await handler.Handle(
        new CustomerCreatedEvent(CustomerId.From(1), UserId.From(42), "test@example.com"),
        CancellationToken.None
    );

    await notifications.Received().SendAsync(UserId.From(42), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
```

### Verifying Publishes in a Service Test

In service-level tests, substitute `IMessageBus` and assert on the recorded calls:

```csharp
[Fact]
public async Task CreateCustomer_publishes_customer_created_event()
{
    var bus = Substitute.For<IMessageBus>();
    var service = new CustomerService(db, bus, NullLogger<CustomerService>.Instance);

    var customer = await service.CreateCustomerAsync(
        new CreateCustomerRequest("Alice", "alice@example.com") { CreatedBy = UserId.From(42) }
    );

    await bus.Received().PublishAsync(Arg.Is<CustomerCreatedEvent>(e => e.CustomerId == customer.Id));
}
```

## Next Steps

- [Permissions](/guide/permissions) — claims-based authorization for endpoints
- [Database](/guide/database) — persistence patterns commonly paired with events
- [Unit Tests](/testing/unit-tests) — how to test event handlers and service-level publishing
