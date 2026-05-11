using Microsoft.Extensions.Logging;
using SimpleModule.Notifications.Contracts;

namespace SimpleModule.Notifications.Channels;

/// <summary>
/// Default SMS channel: writes the message to the log. A real provider (Twilio, etc.)
/// can be plugged in by replacing this registration with another <see cref="INotificationChannel"/>
/// implementation whose <see cref="Name"/> returns <c>"sms"</c>.
/// </summary>
public partial class LogSmsChannel(ILogger<LogSmsChannel> logger) : INotificationChannel
{
    public string Name => NotificationsConstants.Channels.Sms;

    public Task SendAsync(
        NotificationRecipient recipient,
        INotification notification,
        CancellationToken cancellationToken = default
    )
    {
        var sms = notification.ToSms(recipient);
        if (sms is null)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(recipient.PhoneNumber))
        {
            LogMissingPhone(logger, recipient.UserId.Value, notification.NotificationType);
            return Task.CompletedTask;
        }

        LogSms(logger, recipient.PhoneNumber, notification.NotificationType, sms.Body);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[SMS] to={Phone} type={Type} body={Body}"
    )]
    private static partial void LogSms(ILogger logger, string phone, string type, string body);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skipping SMS notification for user {UserId} type {Type}: recipient has no phone number"
    )]
    private static partial void LogMissingPhone(ILogger logger, string userId, string type);
}
