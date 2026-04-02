namespace SimpleModule.BackgroundJobs.Contracts;

public class JobDetailDto
{
    public JobId Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public JobState State { get; set; }
    public int ProgressPercentage { get; set; }
    public string? ProgressMessage { get; set; }
    public string? Error { get; set; }
    public string? Data { get; set; }
    public List<JobLogEntry> Logs { get; set; } = [];
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class JobLogEntry
{
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
