using SimpleModule.Core.Events;

namespace SimpleModule.Core.Entities;

/// <summary>
/// Aggregate root with audit tracking, soft delete, versioning, and domain events.
/// Domain events are automatically dispatched via Wolverine's <c>IMessageBus</c> after SaveChanges.
/// </summary>
public abstract class AuditableAggregateRoot<TId> : FullAuditableEntity<TId>, IHasDomainEvents
{
    private readonly List<IEvent> _domainEvents = [];

    public IReadOnlyList<IEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void AddDomainEvent(IEvent domainEvent) => _domainEvents.Add(domainEvent);
}
