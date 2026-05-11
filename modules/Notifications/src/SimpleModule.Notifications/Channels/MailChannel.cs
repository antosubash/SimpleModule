using Microsoft.Extensions.Logging;
using SimpleModule.Email.Contracts;
using SimpleModule.Notifications.Contracts;

namespace SimpleModule.Notifications.Channels;

/// <summary>
/// Forwards a notification's mail payload to the Email module. Skips silently when the
/// recipient has no email address — channel selection happens upstream in the notifier,
/// but we still defend here so mis-configured callers don't fault.
/// </summary>
public partial class MailChannel(IEmailContracts email, ILogger<MailChannel> logger)
    : INotificationChannel
{
    public string Name => NotificationsConstants.Channels.Mail;

    public async Task SendAsync(
        NotificationRecipient recipient,
        INotification notification,
        CancellationToken cancellationToken = default
    )
    {
        var mail = notification.ToMail(recipient);
        if (mail is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(recipient.Email))
        {
            LogMissingEmail(logger, recipient.UserId.Value, notification.NotificationType);
            return;
        }

        await email.SendEmailAsync(
            new SendEmailRequest
            {
                To = recipient.Email,
                Subject = mail.Subject,
                Body = mail.Body,
                IsHtml = mail.IsHtml,
            }
        );
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skipping mail notification for user {UserId} type {Type}: recipient has no email address"
    )]
    private static partial void LogMissingEmail(ILogger logger, string userId, string type);
}
