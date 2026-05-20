using SimpleModule.Core;

namespace SimpleModule.Notifications.Contracts;

[NoDtoGeneration]
public sealed class DatabaseNotificationPayload
{
    public DatabaseNotificationPayload() { }

    public DatabaseNotificationPayload(string? title, string? body, object? data = null)
    {
        Title = title;
        Body = body;
        Data = data;
    }

    public string? Title { get; set; }
    public string? Body { get; set; }
    public object? Data { get; set; }
}
