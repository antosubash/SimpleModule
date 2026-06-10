using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Services;

public class NotificationService(NotificationsDbContext db)
    : INotificationsContracts,
        INotificationStore
{
    public async Task<PagedResult<Notification>> ListAsync(
        UserId userId,
        QueryNotificationsRequest request
    )
    {
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (request.UnreadOnly == true)
        {
            query = query.Where(n => n.ReadAt == null);
        }
        if (!string.IsNullOrWhiteSpace(request.Channel))
        {
            query = query.Where(n => n.Channel == request.Channel);
        }
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(n => n.Type == request.Type);
        }

        var totalCount = await query.CountAsync();
        var page = request.EffectivePage;
        var pageSize = request.EffectivePageSize;

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Notification>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public Task<int> GetUnreadCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    ) =>
        db.Notifications.CountAsync(
            n => n.UserId == userId && n.ReadAt == null,
            cancellationToken
        );

    public async Task<Notification?> GetByIdAsync(NotificationId id, UserId userId) =>
        await db.Notifications.AsNoTracking().FirstOrDefaultAsync(n =>
            n.Id == id && n.UserId == userId
        );

    public async Task<Notification?> FindAsync(
        NotificationId id,
        CancellationToken cancellationToken = default
    ) =>
        await db.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<bool> MarkReadAsync(NotificationId id, UserId userId)
    {
        // Deliberately load-modify-save rather than ExecuteUpdateAsync: the SaveChanges
        // pipeline runs the framework interceptors (audit log entry, UpdatedAt,
        // concurrency stamp rotation) that a set-based update would silently skip.
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
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent request rotated the concurrency stamp (e.g. a double-click
            // marking the same notification read) — the outcome the caller wanted
            // already holds, so this is an idempotent success, not an error.
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(UserId userId)
    {
        var now = DateTimeOffset.UtcNow;
        return await db
            .Notifications.Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));
    }
}
