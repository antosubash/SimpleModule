using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Contracts;

/// <summary>
/// Identifies a recipient of a notification and supplies the per-channel addresses
/// the channel implementations need. A recipient is keyed by <see cref="UserId"/> so
/// the database channel can persist notifications, while the optional addresses let
/// the mail/sms channels deliver out-of-band.
/// </summary>
[NoDtoGeneration]
public sealed class NotificationRecipient
{
    public NotificationRecipient() { }

    public NotificationRecipient(UserId userId, string? email = null, string? phoneNumber = null)
    {
        UserId = userId;
        Email = email;
        PhoneNumber = phoneNumber;
    }

    public UserId UserId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}
