using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class EmailTemplate
{
    public EmailTemplateId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public string? DefaultReplyTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
