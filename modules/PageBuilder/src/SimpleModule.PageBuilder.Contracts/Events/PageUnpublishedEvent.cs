using SimpleModule.Core.Events;

namespace SimpleModule.PageBuilder.Contracts.Events;

public sealed record PageUnpublishedEvent(PageId PageId, string Title) : IEvent;
