namespace SimpleModule.BackgroundJobs.Contracts;

public class RecurringJobDto
{
    public RecurringJobId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
