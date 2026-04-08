using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs.Contracts;

[NoDtoGeneration]
public sealed record JobQueueEntry(
    Guid Id,
    string JobTypeName,
    string? SerializedData,
    DateTimeOffset ScheduledAt,
    JobQueueEntryState State,
    int AttemptCount,
    string? CronExpression,
    string? RecurringName,
    DateTimeOffset CreatedAt
);
