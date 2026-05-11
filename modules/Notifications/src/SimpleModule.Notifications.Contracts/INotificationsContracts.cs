using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Contracts;

public interface INotificationsContracts
{
    Task<PagedResult<Notification>> ListAsync(UserId userId, QueryNotificationsRequest request);
    Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(NotificationId id, UserId userId);
    Task<bool> MarkReadAsync(NotificationId id, UserId userId);
    Task<int> MarkAllReadAsync(UserId userId);
}
