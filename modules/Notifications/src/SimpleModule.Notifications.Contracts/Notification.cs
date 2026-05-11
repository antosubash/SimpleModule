using SimpleModule.Core;
using SimpleModule.Core.Entities;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Contracts;

[Dto]
public class Notification : Entity<NotificationId>
{
    public UserId UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Body { get; set; }

    // JSON payload — arbitrary, channel-specific or in-app data
    public string DataJson { get; set; } = "{}";

    public DateTimeOffset? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
}
