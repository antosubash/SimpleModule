using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Contracts;

public interface INotificationsContracts
{
    Task<PagedResult<Notification>> ListAsync(UserId userId, QueryNotificationsRequest request);
    Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(NotificationId id, UserId userId);

    /// <summary>
    /// Marks a notification read. Owner-scoped: returns false when the notification
    /// does not exist or belongs to another user — cross-module callers cannot mutate
    /// other users' notifications. Instance-level authorization with richer semantics
    /// (reasons, 404 mapping) lives in <c>NotificationPolicy</c> at the endpoint.
    /// </summary>
    Task<bool> MarkReadAsync(NotificationId id, UserId userId);
    Task<int> MarkAllReadAsync(UserId userId);
}
