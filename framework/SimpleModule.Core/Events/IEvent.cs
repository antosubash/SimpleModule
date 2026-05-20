namespace SimpleModule.Core.Events;

/// <summary>
/// Marker contract for cross-module domain events. Carries a stable identifier and a
/// timestamp so Wolverine's durable inbox can deduplicate redelivery and so audit
/// pipelines have a consistent correlation key. Event records typically inherit
/// <see cref="DomainEvent"/> rather than implementing this directly.
/// </summary>
public interface IEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Base record for domain events. Derive from this to get a unique <see cref="IEvent.EventId"/>
/// and an <see cref="IEvent.OccurredAt"/> stamp without having to repeat the boilerplate
/// on every event record.
/// </summary>
public abstract record DomainEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
