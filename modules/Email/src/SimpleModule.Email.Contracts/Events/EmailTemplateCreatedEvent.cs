using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailTemplateCreatedEvent(EmailTemplateId TemplateId, string Name, string Slug)
    : IEvent;
