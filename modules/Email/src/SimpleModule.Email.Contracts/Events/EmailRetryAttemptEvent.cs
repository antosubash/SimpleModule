using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailRetryAttemptEvent(EmailMessageId MessageId, string To, int RetryCount)
    : IEvent;
