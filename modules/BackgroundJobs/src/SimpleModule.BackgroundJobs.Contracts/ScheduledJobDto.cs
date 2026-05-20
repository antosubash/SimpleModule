using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs.Contracts;

[Dto]
public class ScheduledJobDto
{
    public string Name { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "UTC";
    public bool WithoutOverlapping { get; set; }
    public bool OnOneServer { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
}
