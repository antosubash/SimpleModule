using SimpleModule.Core.Events;

namespace SimpleModule.PageBuilder.Contracts.Events;

public sealed record PagePublishedEvent(PageId PageId, string Title) : IEvent;
