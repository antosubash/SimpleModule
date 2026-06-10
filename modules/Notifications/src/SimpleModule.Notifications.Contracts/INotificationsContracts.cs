using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Contracts;

public interface INotificationsContracts
{
    Task<PagedResult<Notification>> ListAsync(UserId userId, QueryNotificationsRequest request);
    Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(NotificationId id, UserId userId);

    /// <summary>
    /// Loads a notification without owner scoping. Callers are responsible for the
    /// instance-level check via <c>IAuthorizer</c> + <c>NotificationPolicy</c>.
    /// </summary>
    Task<Notification?> FindAsync(NotificationId id);

    /// <summary>
    /// Marks a notification read. Authorization happens at the endpoint via
    /// <c>NotificationPolicy</c> — this method assumes the caller is allowed.
    /// </summary>
    Task MarkReadAsync(NotificationId id);
    Task<int> MarkAllReadAsync(UserId userId);
}
