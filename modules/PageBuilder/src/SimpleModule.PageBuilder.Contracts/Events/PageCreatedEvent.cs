using SimpleModule.Core.Events;

namespace SimpleModule.PageBuilder.Contracts.Events;

public sealed record PageCreatedEvent(PageId PageId, string Title, string Slug) : IEvent;
