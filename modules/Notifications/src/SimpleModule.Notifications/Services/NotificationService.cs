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
        // Single owner-scoped statement; the coalesce keeps an existing ReadAt so the
        // call stays idempotent. Affected == 0 means missing or not owned by the caller.
        var now = DateTimeOffset.UtcNow;
        var affected = await db
            .Notifications.Where(n => n.Id == id && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, n => n.ReadAt ?? now));
        return affected > 0;
    }

    public async Task<int> MarkAllReadAsync(UserId userId)
    {
        var now = DateTimeOffset.UtcNow;
        return await db
            .Notifications.Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));
    }
}
