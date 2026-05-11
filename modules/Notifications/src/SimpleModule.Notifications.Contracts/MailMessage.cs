using SimpleModule.Core;

namespace SimpleModule.Notifications.Contracts;

[NoDtoGeneration]
public sealed class MailMessage
{
    public MailMessage() { }

    public MailMessage(string subject, string body, bool isHtml = false)
    {
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
    }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
}
