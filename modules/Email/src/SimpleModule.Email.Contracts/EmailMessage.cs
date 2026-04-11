using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.Email.Contracts;

[Dto]
public class EmailMessage : Entity<EmailMessageId>
{
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? ReplyTo { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public EmailStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? TemplateSlug { get; set; }
    public string? Provider { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
