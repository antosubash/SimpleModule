namespace SimpleModule.Notifications.Contracts;

/// <summary>
/// High-level send API. <see cref="SendAsync"/> queues delivery through the background
/// jobs system so the caller does not block on slow channels. <see cref="SendNowAsync"/>
/// dispatches synchronously and is intended for tests and tightly-coupled flows where
/// the caller wants to surface errors immediately.
/// </summary>
public interface INotifier
{
    Task SendAsync<T>(
        NotificationRecipient recipient,
        T notification,
        CancellationToken cancellationToken = default
    )
        where T : INotification;

    Task SendNowAsync<T>(
        NotificationRecipient recipient,
        T notification,
        CancellationToken cancellationToken = default
    )
        where T : INotification;
}
