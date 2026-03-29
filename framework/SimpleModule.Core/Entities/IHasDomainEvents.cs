using SimpleModule.Core.Events;

namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface can raise domain events that are automatically
/// dispatched via <see cref="IEventBus"/> after a successful SaveChanges.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IEvent> GetDomainEvents();
    void ClearDomainEvents();
}
