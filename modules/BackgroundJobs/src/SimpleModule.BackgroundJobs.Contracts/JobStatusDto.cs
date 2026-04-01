namespace SimpleModule.BackgroundJobs.Contracts;

public record JobStatusDto(
    JobId Id,
    string JobType,
    JobState State,
    int ProgressPercentage,
    string? ProgressMessage,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int RetryCount
);
