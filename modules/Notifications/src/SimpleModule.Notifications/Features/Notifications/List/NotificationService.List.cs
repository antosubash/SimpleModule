using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Contracts.Features.Notifications.List;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
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
}
