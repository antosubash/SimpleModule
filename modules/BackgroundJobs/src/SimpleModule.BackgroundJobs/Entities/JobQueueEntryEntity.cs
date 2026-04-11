using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core.Entities;

namespace SimpleModule.BackgroundJobs.Entities;

public class JobQueueEntryEntity : Entity<JobId>
{
    public string JobTypeName { get; set; } = string.Empty;
    public string? SerializedData { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public JobQueueEntryState State { get; set; }
    public string? ClaimedBy { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CronExpression { get; set; }
    public string? RecurringName { get; set; }
}
