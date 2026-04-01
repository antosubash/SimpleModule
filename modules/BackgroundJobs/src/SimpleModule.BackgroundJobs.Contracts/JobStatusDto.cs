namespace SimpleModule.BackgroundJobs.Contracts;

public class JobStatusDto
{
    public JobId Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public JobState State { get; set; }
    public int ProgressPercentage { get; set; }
    public string? ProgressMessage { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int RetryCount { get; set; }
}
