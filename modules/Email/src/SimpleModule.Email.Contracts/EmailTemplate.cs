using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.Email.Contracts;

[Dto]
public class EmailTemplate : Entity<EmailTemplateId>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public string? DefaultReplyTo { get; set; }
}
