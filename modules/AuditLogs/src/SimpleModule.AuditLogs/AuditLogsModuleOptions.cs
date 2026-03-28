using SimpleModule.Core;

namespace SimpleModule.AuditLogs;

/// <summary>
/// Configurable options for the AuditLogs module.
/// </summary>
public class AuditLogsModuleOptions : IModuleOptions
{
    /// <summary>
    /// Maximum number of audit entries to batch before flushing to the database. Default: 100.
    /// </summary>
    public int WriterBatchSize { get; set; } = 100;

    /// <summary>
    /// How long to wait before flushing an incomplete batch. Default: 2 seconds.
    /// </summary>
    public TimeSpan WriterFlushInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Default number of days to retain audit log entries before cleanup. Default: 90.
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// How often the retention cleanup job runs. Default: 24 hours.
    /// </summary>
    public TimeSpan RetentionCheckInterval { get; set; } = TimeSpan.FromHours(24);
}
