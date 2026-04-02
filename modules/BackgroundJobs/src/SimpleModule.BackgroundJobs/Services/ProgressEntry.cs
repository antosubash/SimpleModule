namespace SimpleModule.BackgroundJobs.Services;

public record ProgressEntry(
    Guid JobId,
    int Percentage,
    string? Message,
    string? LogMessage,
    DateTimeOffset Timestamp
);
