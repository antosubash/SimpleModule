using Microsoft.Extensions.Logging;

namespace SimpleModule.Notifications.Infrastructure;

internal static partial class NotificationsLog
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Notification channel {Channel} failed for type {Type} user {UserId}"
    )]
    public static partial void ChannelFailure(
        ILogger logger,
        string channel,
        string type,
        string userId,
        Exception exception
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Unknown notification channel '{Channel}' requested for type {Type}; skipping"
    )]
    public static partial void UnknownChannel(ILogger logger, string channel, string type);
}
