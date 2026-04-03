using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailSentEvent(EmailMessageId MessageId, string To, string Subject) : IEvent;
