using SimpleModule.Core;

namespace SimpleModule.AuditLogs.Contracts;

public interface IAuditLogContracts
{
    Task<PagedResult<AuditEntry>> QueryAsync(AuditQueryRequest request);
    Task<AuditEntry?> GetByIdAsync(AuditEntryId id);
    Task<IReadOnlyList<AuditEntry>> GetByCorrelationIdAsync(Guid correlationId);
    Task<Stream> ExportAsync(AuditExportRequest request);
    Task<AuditStats> GetStatsAsync(DateTimeOffset from, DateTimeOffset to);
    Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries);
    Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff);
}
