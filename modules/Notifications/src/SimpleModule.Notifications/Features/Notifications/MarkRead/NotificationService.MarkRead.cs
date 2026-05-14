using Microsoft.EntityFrameworkCore;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
{
    public async Task<bool> MarkReadAsync(NotificationId id, UserId userId)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(n =>
            n.Id == id && n.UserId == userId
        );
        if (notification is null)
        {
            return false;
        }

        if (notification.ReadAt is not null)
        {
            return true;
        }

        notification.ReadAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}
