using SimpleModule.Core.Events;

namespace SimpleModule.Core.Entities;

/// <summary>
/// Aggregate root with audit tracking, soft delete, versioning, and domain events.
/// Domain events added via <see cref="AddDomainEvent"/> are flushed to Wolverine's
/// durable outbox during <c>SaveChangesAsync</c>, atomic with the EF transaction.
/// </summary>
public abstract class AuditableAggregateRoot<TId> : FullAuditableEntity<TId>, IHasDomainEvents
{
    public List<IEvent> Events { get; } = [];

    protected void AddDomainEvent(IEvent domainEvent) => Events.Add(domainEvent);
}
