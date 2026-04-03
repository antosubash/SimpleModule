using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailTemplateUpdatedEvent(
    EmailTemplateId TemplateId,
    string Name,
    IReadOnlyList<string> ChangedFields
) : IEvent;
