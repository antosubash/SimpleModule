using SimpleModule.Core.Events;

namespace SimpleModule.PageBuilder.Contracts.Events;

public sealed record PageDeletedEvent(PageId PageId) : IEvent;
