using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;

namespace SimpleModule.AuditLogs;

public sealed partial class AuditLogService(AuditLogsDbContext db, ILogger<AuditLogService> logger)
    : IAuditLogContracts
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
        var entries = db
            .AuditEntries.Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .AsNoTracking();

        var totalEntries = await entries.CountAsync();
        var uniqueUsers = await entries
            .Where(e => e.UserId != null)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync();

        var byModule = await entries
            .Where(e => e.Module != null)
            .GroupBy(e => e.Module!)
            .Select(g => new { Module = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Module, x => x.Count);

        var byAction = await entries
            .Where(e => e.Action != null)
            .GroupBy(e => e.Action!.Value)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action.ToString(), x => x.Count);

        var byStatusCode = await entries
            .Where(e => e.StatusCode != null)
            .GroupBy(e => e.StatusCode!.Value)
            .Select(g => new { StatusCode = g.Key, Count = g.Count() })
            .ToDictionaryAsync(
                x => x.StatusCode.ToString(CultureInfo.InvariantCulture),
                x => x.Count
            );

        return new AuditStats
        {
            TotalEntries = totalEntries,
            UniqueUsers = uniqueUsers,
            ByModule = byModule,
            ByAction = byAction,
            ByStatusCode = byStatusCode,
        };
    }

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
