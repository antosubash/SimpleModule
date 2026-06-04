using SimpleModule.Notifications.Contracts;

namespace SimpleModule.Notifications.Infrastructure.Channels;

/// <summary>
/// A delivery channel — mail, database, sms, slack, push, etc. Each channel
/// implementation is registered in DI by name and the notifier dispatches to it
/// based on what <see cref="INotification.Via"/> returns.
/// </summary>
/// <remarks>
/// Lives in the implementation assembly (not <c>SimpleModule.Notifications.Contracts</c>)
/// because each channel is a per-module-internal extension point with many implementations,
/// which the source generator's contract rules forbid in a Contracts assembly. Optional
/// out-of-tree channels (Slack, push, custom SMS providers) reference this assembly
/// directly the same way Email providers reference <c>SimpleModule.Email</c>.
/// </remarks>
public interface INotificationChannel
{
    /// <summary>Channel identifier matching the names returned from <see cref="INotification.Via"/>.</summary>
    string Name { get; }

    Task SendAsync(
        NotificationRecipient recipient,
        INotification notification,
        CancellationToken cancellationToken = default
    );
}
