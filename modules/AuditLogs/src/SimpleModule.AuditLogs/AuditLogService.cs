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

    public async Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries)
    {
        db.AuditEntries.AddRange(entries);
        await db.SaveChangesAsync();
        LogBatchWritten(logger, entries.Count);
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Wrote batch of {Count} audit entries")]
    private static partial void LogBatchWritten(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged {Count} old audit entries")]
    private static partial void LogPurged(ILogger logger, int count);
}
