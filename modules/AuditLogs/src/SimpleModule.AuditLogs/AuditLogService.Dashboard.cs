using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs;

public sealed partial class AuditLogService
{
    public async Task<AuditStats> GetStatsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        var dashboard = await GetDashboardStatsAsync(from, to);
        return new AuditStats
        {
            TotalEntries = dashboard.TotalEntries,
            UniqueUsers = dashboard.UniqueUsers,
            ByModule = dashboard.ByModule,
            ByAction = dashboard.ByAction,
            ByStatusCode = dashboard.ByStatusCategory,
        };
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? userId = null
    )
    {
        var entries = await QueryByTimeRangeAsync(from, to, userId);

        var totalEntries = entries.Count;
        var uniqueUsers = entries
            .Where(e => e.UserId is not null)
            .Select(e => e.UserId)
            .Distinct()
            .Count();

        long durationSum = 0;
        int durationCount = 0;
        int statusTotal = 0;
        int statusErrors = 0;
        foreach (var e in entries)
        {
            if (e.DurationMs.HasValue)
            {
                durationSum += e.DurationMs.Value;
                durationCount++;
            }
            if (e.StatusCode.HasValue)
            {
                statusTotal++;
                if (e.StatusCode.Value >= 400)
                    statusErrors++;
            }
        }
        var averageDuration = durationCount > 0 ? (double)durationSum / durationCount : 0;
        var errorRate = statusTotal > 0 ? (double)statusErrors / statusTotal * 100 : 0;

        var bySource = entries
            .GroupBy(e => e.Source)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var byAction = entries
            .Where(e => e.Action.HasValue)
            .GroupBy(e => e.Action!.Value)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var byModule = entries
            .Where(e => e.Module is not null)
            .GroupBy(e => e.Module!)
            .ToDictionary(g => g.Key, g => g.Count());

        var byStatusCategory = entries
            .Where(e => e.StatusCode.HasValue)
            .GroupBy(e =>
                e.StatusCode!.Value switch
                {
                    >= 200 and < 300 => "2xx",
                    >= 300 and < 400 => "3xx",
                    >= 400 and < 500 => "4xx",
                    >= 500 => "5xx",
                    _ => "Other",
                }
            )
            .ToDictionary(g => g.Key, g => g.Count());

        var byEntityType = entries
            .Where(e => e.EntityType is not null)
            .GroupBy(e => e.EntityType!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        var topUsers = entries
            .Where(e => e.UserId is not null)
            .GroupBy(e => e.UserName ?? e.UserId!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
            .ToList();

        var topPaths = entries
            .Where(e => e.Path is not null)
            .GroupBy(e => e.Path!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
            .ToList();

        var timeline = entries
            .GroupBy(e => e.Timestamp.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TimelinePoint
            {
                Date = g.Key.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                Http = g.Count(e => e.Source == AuditSource.Http),
                Domain = g.Count(e => e.Source == AuditSource.Domain),
                Changes = g.Count(e => e.Source == AuditSource.ChangeTracker),
            })
            .ToList();

        var hourlyDistribution = entries
            .GroupBy(e => e.Timestamp.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new NamedCount
            {
                Name =
                    g.Key.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + ":00",
                Count = g.Count(),
            })
            .ToList();

        return new DashboardStats
        {
            TotalEntries = totalEntries,
            UniqueUsers = uniqueUsers,
            AverageDurationMs = Math.Round(averageDuration, 1),
            ErrorRate = Math.Round(errorRate, 1),
            BySource = bySource,
            ByAction = byAction,
            ByModule = byModule,
            ByStatusCategory = byStatusCategory,
            ByEntityType = byEntityType,
            TopUsers = topUsers,
            TopPaths = topPaths,
            Timeline = timeline,
            HourlyDistribution = hourlyDistribution,
        };
    }
}
