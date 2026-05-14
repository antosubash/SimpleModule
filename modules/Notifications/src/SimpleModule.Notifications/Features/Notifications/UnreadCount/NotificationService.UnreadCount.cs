using Microsoft.EntityFrameworkCore;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
{
    public Task<int> GetUnreadCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    ) =>
        db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, cancellationToken);
}
