using SimpleModule.Core.Events;

namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface have their <see cref="Events"/> list scraped
/// by Wolverine's <c>PublishDomainEventsFromEntityFrameworkCore</c> integration during
/// <c>SaveChangesAsync</c> — events are written to the outbox in the same transaction
/// as the EF business write, and the list is cleared after scrape.
/// </summary>
public interface IHasDomainEvents
{
    List<IEvent> Events { get; }
}
