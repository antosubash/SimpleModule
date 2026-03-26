using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Database;

namespace SimpleModule.AuditLogs;

public sealed partial class AuditLogService(
    AuditLogsDbContext db,
    IOptions<DatabaseOptions> dbOptions,
    ILogger<AuditLogService> logger
) : IAuditLogContracts
{
    private static readonly JsonSerializerOptions s_exportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<PagedResult<AuditEntry>> QueryAsync(AuditQueryRequest request)
    {
        var query = BuildQuery(request);

        var totalCount = await query.CountAsync();

        var sortBy = request.EffectiveSortBy;
        var sortDesc = request.EffectiveSortDescending;
        var page = request.EffectivePage;
        var pageSize = request.EffectivePageSize;

        // Apply sorting
        query = sortBy switch
        {
            "UserId" => sortDesc
                ? query.OrderByDescending(e => e.UserId)
                : query.OrderBy(e => e.UserId),
            "Module" => sortDesc
                ? query.OrderByDescending(e => e.Module)
                : query.OrderBy(e => e.Module),
            "Path" => sortDesc ? query.OrderByDescending(e => e.Path) : query.OrderBy(e => e.Path),
            "StatusCode" => sortDesc
                ? query.OrderByDescending(e => e.StatusCode)
                : query.OrderBy(e => e.StatusCode),
            "DurationMs" => sortDesc
                ? query.OrderByDescending(e => e.DurationMs)
                : query.OrderBy(e => e.DurationMs),
            // SQLite does not support DateTimeOffset in ORDER BY, so sort by Id
            // (auto-increment, correlates with insertion order) as a fallback.
            _ => sortDesc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return new PagedResult<AuditEntry>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AuditEntry?> GetByIdAsync(AuditEntryId id) =>
        await db.AuditEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<AuditEntry>> GetByCorrelationIdAsync(Guid correlationId) =>
        await db
            .AuditEntries.Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Stream> ExportAsync(AuditExportRequest request)
    {
        var query = BuildQuery(request);
        var entries = await query.OrderByDescending(e => e.Id).AsNoTracking().ToListAsync();

        return request.EffectiveFormat.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? ExportAsJson(entries)
            : ExportAsCsv(entries);
    }

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

    public async Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries)
    {
        db.AuditEntries.AddRange(entries);
        await db.SaveChangesAsync();
        LogBatchWritten(logger, entries.Count);
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

    public async Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff)
    {
        var count = await db.AuditEntries.Where(e => e.Timestamp < cutoff).ExecuteDeleteAsync();
        LogPurged(logger, count);
        return count;
    }

    /// <summary>
    /// Fetches audit entries within a time range, with optional userId filter.
    /// SQLite can't translate DateTimeOffset comparisons in SQL, so we fall back
    /// to loading all rows and filtering in memory.
    /// </summary>
    private async Task<List<AuditEntry>> QueryByTimeRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? userId = null
    )
    {
        var provider = DatabaseProviderDetector.Detect(
            dbOptions.Value.DefaultConnection,
            dbOptions.Value.Provider
        );

        List<AuditEntry> entries;

        if (provider == DatabaseProvider.Sqlite)
        {
            var allEntries = await db.AuditEntries.AsNoTracking().ToListAsync();
            entries = allEntries.Where(e => e.Timestamp >= from && e.Timestamp <= to).ToList();
        }
        else
        {
            entries = await db
                .AuditEntries.Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .AsNoTracking()
                .ToListAsync();
        }

        if (!string.IsNullOrWhiteSpace(userId))
            entries = entries.Where(e => e.UserId == userId).ToList();

        return entries;
    }

    private IQueryable<AuditEntry> BuildQuery(AuditQueryRequest request)
    {
        IQueryable<AuditEntry> query = db.AuditEntries;

        if (request.From.HasValue)
            query = query.Where(e => e.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(e => e.Timestamp <= request.To.Value);

        if (!string.IsNullOrWhiteSpace(request.UserId))
            query = query.Where(e => e.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Module))
            query = query.Where(e => e.Module == request.Module);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(e => e.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(e => e.EntityId == request.EntityId);

        if (request.Source.HasValue)
            query = query.Where(e => e.Source == request.Source.Value);

        if (request.Action.HasValue)
            query = query.Where(e => e.Action == request.Action.Value);

        if (request.StatusCode.HasValue)
            query = query.Where(e => e.StatusCode == request.StatusCode.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var search = request.SearchText;
            query = query.Where(e =>
                (e.Path != null && e.Path.Contains(search))
                || (e.Changes != null && e.Changes.Contains(search))
                || (e.Metadata != null && e.Metadata.Contains(search))
                || (e.UserName != null && e.UserName.Contains(search))
            );
        }

        return query;
    }

    private static MemoryStream ExportAsCsv(List<AuditEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Timestamp,Source,UserId,UserName,HttpMethod,Path,StatusCode,DurationMs,Module,EntityType,EntityId,Action,Changes"
        );
        foreach (var e in entries)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{e.Timestamp:O},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.Source},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.UserId)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.UserName)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.HttpMethod},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.Path)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.StatusCode},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.DurationMs},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.Module)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.EntityType)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.EntityId)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.Action},");
            sb.AppendLine(CsvEscape(e.Changes));
        }
        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static MemoryStream ExportAsJson(List<AuditEntry> entries)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(entries, s_exportJsonOptions);
        return new MemoryStream(json);
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (
            value.Contains('"', StringComparison.Ordinal)
            || value.Contains(',', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal)
        )
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Wrote batch of {Count} audit entries")]
    private static partial void LogBatchWritten(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged {Count} old audit entries")]
    private static partial void LogPurged(ILogger logger, int count);
}
