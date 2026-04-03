using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailFailedEvent(
    EmailMessageId MessageId,
    string To,
    string Subject,
    string Error
) : IEvent;
