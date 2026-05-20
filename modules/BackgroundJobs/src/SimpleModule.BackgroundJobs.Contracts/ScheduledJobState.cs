using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.BackgroundJobs.Contracts;

/// <summary>
/// Persisted state for a code-declared scheduled job. Keyed by <see cref="Name"/>;
/// the scheduler service upserts on reconcile and bumps <c>LastRunAt</c>/<c>NextRunAt</c>
/// on every tick.
/// </summary>
[NoDtoGeneration]
public class ScheduledJobState : IHasCreationTime, IHasModificationTime, IHasConcurrencyStamp
{
    public string Name { get; set; } = string.Empty;
    public string JobTypeName { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "UTC";
    public string? Payload { get; set; }
    public bool WithoutOverlapping { get; set; }
    public bool OnOneServer { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
