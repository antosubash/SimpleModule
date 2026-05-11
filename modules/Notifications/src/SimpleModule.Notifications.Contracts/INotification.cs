namespace SimpleModule.Notifications.Contracts;

/// <summary>
/// A notification that can be dispatched to one or more channels for a given recipient.
/// Implement <see cref="Via"/> to declare which channel names should attempt delivery
/// and override the per-channel <c>To*</c> methods to supply the channel-specific payload.
/// </summary>
public interface INotification
{
    /// <summary>Stable identifier for this notification type (e.g. <c>"orders.shipped"</c>).</summary>
    string NotificationType { get; }

    /// <summary>Channel names to deliver this notification on (see <see cref="NotificationsConstants.Channels"/>).</summary>
    string[] Via(NotificationRecipient recipient);

    MailMessage? ToMail(NotificationRecipient recipient) => null;
    DatabaseNotificationPayload? ToDatabase(NotificationRecipient recipient) => null;
    SmsMessage? ToSms(NotificationRecipient recipient) => null;
}
