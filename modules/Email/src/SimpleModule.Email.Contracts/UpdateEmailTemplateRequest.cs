using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class UpdateEmailTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public string? DefaultReplyTo { get; set; }
}
