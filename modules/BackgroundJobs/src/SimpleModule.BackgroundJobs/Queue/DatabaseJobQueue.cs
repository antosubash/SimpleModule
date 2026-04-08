// modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Queue/DatabaseJobQueue.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Entities;

namespace SimpleModule.BackgroundJobs.Queue;

public sealed partial class DatabaseJobQueue(
    BackgroundJobsDbContext db,
    ILogger<DatabaseJobQueue> logger
) : IJobQueue
{
    public async Task EnqueueAsync(JobQueueEntry entry, CancellationToken ct = default)
    {
        var row = new JobQueueEntryEntity
        {
            Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            JobTypeName = entry.JobTypeName,
            SerializedData = entry.SerializedData,
            ScheduledAt = entry.ScheduledAt,
            State = JobQueueEntryState.Pending,
            AttemptCount = entry.AttemptCount,
            CronExpression = entry.CronExpression,
            RecurringName = entry.RecurringName,
            CreatedAt = entry.CreatedAt == default ? DateTimeOffset.UtcNow : entry.CreatedAt,
        };

        db.JobQueueEntries.Add(row);
        await db.SaveChangesAsync(ct);
        LogEnqueued(logger, row.Id, row.JobTypeName);
    }

    public async Task<JobQueueEntry?> DequeueAsync(string workerId, CancellationToken ct = default)
    {
        var isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        var now = DateTimeOffset.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        JobQueueEntryEntity? candidate;
        if (isPostgres)
        {
            // Use FOR UPDATE SKIP LOCKED for concurrent claim safety on Postgres.
            var sql = """
                SELECT * FROM "JobQueueEntries"
                WHERE "State" = 0 AND "ScheduledAt" <= {0}
                ORDER BY "ScheduledAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """;
            candidate = await db.JobQueueEntries
                .FromSqlRaw(sql, now)
                .AsTracking()
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            candidate = await db.JobQueueEntries
                .Where(e => e.State == JobQueueEntryState.Pending && e.ScheduledAt <= now)
                .OrderBy(e => e.ScheduledAt)
                .FirstOrDefaultAsync(ct);
        }

        if (candidate is null)
        {
            await tx.CommitAsync(ct);
            return null;
        }

        candidate.State = JobQueueEntryState.Claimed;
        candidate.ClaimedBy = workerId;
        candidate.ClaimedAt = DateTimeOffset.UtcNow;
        candidate.AttemptCount += 1;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        LogClaimed(logger, candidate.Id, workerId);

        return new JobQueueEntry(
            candidate.Id,
            candidate.JobTypeName,
            candidate.SerializedData,
            candidate.ScheduledAt,
            candidate.State,
            candidate.AttemptCount,
            candidate.CronExpression,
            candidate.RecurringName,
            candidate.CreatedAt
        );
    }

    public async Task CompleteAsync(Guid entryId, CancellationToken ct = default)
    {
        var row = await db.JobQueueEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (row is null) return;
        row.State = JobQueueEntryState.Completed;
        row.CompletedAt = DateTimeOffset.UtcNow;
        row.Error = null;
        await db.SaveChangesAsync(ct);
        LogCompleted(logger, entryId);
    }

    public async Task FailAsync(Guid entryId, string error, CancellationToken ct = default)
    {
        var row = await db.JobQueueEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (row is null) return;
        row.State = JobQueueEntryState.Failed;
        row.CompletedAt = DateTimeOffset.UtcNow;
        row.Error = error;
        await db.SaveChangesAsync(ct);
        LogFailed(logger, entryId, error);
    }

    public async Task<int> RequeueStalledAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - timeout;
        var stalled = await db.JobQueueEntries
            .Where(e => e.State == JobQueueEntryState.Claimed && e.ClaimedAt != null && e.ClaimedAt < cutoff)
            .ToListAsync(ct);

        foreach (var row in stalled)
        {
            row.State = JobQueueEntryState.Pending;
            row.ClaimedBy = null;
            row.ClaimedAt = null;
        }

        if (stalled.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            LogRequeued(logger, stalled.Count);
        }
        return stalled.Count;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Enqueued job {EntryId} ({JobType})")]
    private static partial void LogEnqueued(ILogger logger, Guid entryId, string jobType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Claimed job {EntryId} by {WorkerId}")]
    private static partial void LogClaimed(ILogger logger, Guid entryId, string workerId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Completed job {EntryId}")]
    private static partial void LogCompleted(ILogger logger, Guid entryId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed job {EntryId}: {Error}")]
    private static partial void LogFailed(ILogger logger, Guid entryId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Requeued {Count} stalled job(s)")]
    private static partial void LogRequeued(ILogger logger, int count);
}
