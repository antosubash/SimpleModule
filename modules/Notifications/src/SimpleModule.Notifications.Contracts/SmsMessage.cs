using SimpleModule.Core;

namespace SimpleModule.Notifications.Contracts;

[NoDtoGeneration]
public sealed class SmsMessage
{
    public SmsMessage() { }

    public SmsMessage(string body)
    {
        Body = body;
    }

    public string Body { get; set; } = string.Empty;
}
