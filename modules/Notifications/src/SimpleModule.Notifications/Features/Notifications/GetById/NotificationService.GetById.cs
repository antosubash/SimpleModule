using Microsoft.EntityFrameworkCore;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
{
    public async Task<Notification?> GetByIdAsync(NotificationId id, UserId userId) =>
        await db
            .Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
}
