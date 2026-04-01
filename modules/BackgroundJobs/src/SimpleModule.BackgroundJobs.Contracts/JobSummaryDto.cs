namespace SimpleModule.BackgroundJobs.Contracts;

public class JobSummaryDto
{
    public JobId Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public JobState State { get; set; }
    public int ProgressPercentage { get; set; }
    public string? ProgressMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
