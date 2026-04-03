using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailTemplateDeletedEvent(EmailTemplateId TemplateId, string Name) : IEvent;
