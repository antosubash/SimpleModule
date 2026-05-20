using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs.Contracts;

/// <summary>
/// In-memory description of a code-declared scheduled job. Populated by the
/// <see cref="IScheduledJob{TJob}"/> fluent builder and consumed by
/// <c>SchedulerService</c> at tick time.
/// </summary>
[NoDtoGeneration]
public sealed class ScheduledJobDefinition
{
    public string Name { get; set; } = string.Empty;
    public Type JobType { get; set; } = null!;
    public string CronExpression { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "UTC";
    public object? Payload { get; set; }
    public bool WithoutOverlapping { get; set; }
    public bool OnOneServer { get; set; }
}
