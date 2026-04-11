// modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Services/BackgroundJobsService.cs
using System.Text.Json;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Entities;
using static SimpleModule.BackgroundJobs.BackgroundJobsInternalConstants;

namespace SimpleModule.BackgroundJobs.Services;

public sealed partial class BackgroundJobsService(
    IJobQueue queue,
    BackgroundJobsDbContext db,
    ILogger<BackgroundJobsService> logger
) : IBackgroundJobs
{
    public async Task<JobId> EnqueueAsync<TJob>(object? data, CancellationToken ct)
        where TJob : IModuleJob
    {
        var jobType = typeof(TJob);
        var id = Guid.NewGuid();
        var serialized = data is not null ? JsonSerializer.Serialize(data) : null;
        var now = DateTimeOffset.UtcNow;

        await queue.EnqueueAsync(
            new JobQueueEntry(
                id,
                jobType.AssemblyQualifiedName!,
                serialized,
                now,
                JobQueueEntryState.Pending,
                0,
                null,
                null,
                now
            ),
            ct
        );

        await CreateJobProgressAsync(id, jobType, serialized, ct);
        LogJobEnqueued(logger, jobType.Name, id);
        return JobId.From(id);
    }

    public async Task<JobId> ScheduleAsync<TJob>(
        DateTimeOffset executeAt,
        object? data,
        CancellationToken ct
    )
        where TJob : IModuleJob
    {
        var jobType = typeof(TJob);
        var id = Guid.NewGuid();
        var serialized = data is not null ? JsonSerializer.Serialize(data) : null;

        await queue.EnqueueAsync(
            new JobQueueEntry(
                id,
                jobType.AssemblyQualifiedName!,
                serialized,
                executeAt,
                JobQueueEntryState.Pending,
                0,
                null,
                null,
                DateTimeOffset.UtcNow
            ),
            ct
        );

        await CreateJobProgressAsync(id, jobType, serialized, ct);
        LogJobScheduled(logger, jobType.Name, id, executeAt);
        return JobId.From(id);
    }

    public async Task<RecurringJobId> AddRecurringAsync<TJob>(
        string name,
        string cronExpression,
        object? data,
        CancellationToken ct
    )
        where TJob : IModuleJob
    {
        // Validate cron expression
        var format =
            cronExpression.Split(' ').Length > 5 ? CronFormat.IncludeSeconds : CronFormat.Standard;
        var cron = CronExpression.Parse(cronExpression, format);
        var next =
            cron.GetNextOccurrence(DateTime.UtcNow, inclusive: false)
            ?? throw new InvalidOperationException(
                $"Cron '{cronExpression}' has no next occurrence."
            );

        // Remove any existing recurring with the same name to keep it unique
        var existing = await db
            .JobQueueEntries.Where(e =>
                e.RecurringName == name && e.State == JobQueueEntryState.Pending
            )
            .ToListAsync(ct);
        db.JobQueueEntries.RemoveRange(existing);
        if (existing.Count > 0)
            await db.SaveChangesAsync(ct);

        var jobType = typeof(TJob);
        var id = Guid.NewGuid();
        var serialized = data is not null ? JsonSerializer.Serialize(data) : null;

        await queue.EnqueueAsync(
            new JobQueueEntry(
                id,
                jobType.AssemblyQualifiedName!,
                serialized,
                new DateTimeOffset(next, TimeSpan.Zero),
                JobQueueEntryState.Pending,
                0,
                cronExpression,
                name,
                DateTimeOffset.UtcNow
            ),
            ct
        );

        LogRecurringJobAdded(logger, name, cronExpression);
        return RecurringJobId.From(id);
    }

    public async Task RemoveRecurringAsync(RecurringJobId id, CancellationToken ct)
    {
        var jobId = JobId.From(id.Value);
        var row = await db.JobQueueEntries.FirstOrDefaultAsync(e => e.Id == jobId, ct);
        if (row is null)
            return;
        var name = row.RecurringName;
        if (name is not null)
        {
            var all = await db.JobQueueEntries.Where(e => e.RecurringName == name).ToListAsync(ct);
            db.JobQueueEntries.RemoveRange(all);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ToggleRecurringAsync(RecurringJobId id, CancellationToken ct)
    {
        var jobId = JobId.From(id.Value);
        // Toggle: if Pending → set ScheduledAt far future (disabled). If "disabled" → reset to next cron occurrence.
        var row =
            await db.JobQueueEntries.FirstOrDefaultAsync(e => e.Id == jobId, ct)
            ?? throw new InvalidOperationException($"Recurring job {id} not found.");

        var disabledSentinel = DateTimeOffset.MaxValue.AddDays(-1);
        var isDisabled = row.ScheduledAt >= disabledSentinel.AddYears(-1);

        if (isDisabled && row.CronExpression is not null)
        {
            var format =
                row.CronExpression.Split(' ').Length > 5
                    ? CronFormat.IncludeSeconds
                    : CronFormat.Standard;
            var cron = CronExpression.Parse(row.CronExpression, format);
            var next = cron.GetNextOccurrence(DateTime.UtcNow, inclusive: false);
            row.ScheduledAt = next.HasValue
                ? new DateTimeOffset(next.Value, TimeSpan.Zero)
                : DateTimeOffset.UtcNow;
        }
        else
        {
            row.ScheduledAt = disabledSentinel;
        }
        await db.SaveChangesAsync(ct);
        return !isDisabled ? false : true;
    }

    public async Task CancelAsync(JobId jobId, CancellationToken ct)
    {
        var row = await db.JobQueueEntries.FirstOrDefaultAsync(e => e.Id == jobId, ct);
        if (row is null || row.State != JobQueueEntryState.Pending)
            return;
        db.JobQueueEntries.Remove(row);
        await db.SaveChangesAsync(ct);
    }

    public async Task<JobStatusDto?> GetStatusAsync(JobId jobId, CancellationToken ct)
    {
        var row = await db
            .JobQueueEntries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == jobId, ct);
        if (row is null)
            return null;

        var progress = await db
            .JobProgress.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        return new JobStatusDto
        {
            Id = jobId,
            JobType = GetShortTypeName(row.JobTypeName),
            State = MapQueueState(row.State),
            ProgressPercentage =
                progress?.ProgressPercentage
                ?? (row.State == JobQueueEntryState.Completed ? 100 : 0),
            ProgressMessage = progress?.ProgressMessage,
            Error = row.Error,
            CreatedAt = row.CreatedAt,
            StartedAt = row.ClaimedAt,
            CompletedAt = row.CompletedAt,
            RetryCount = Math.Max(0, row.AttemptCount - 1),
        };
    }

    public static JobState MapQueueState(JobQueueEntryState state) =>
        state switch
        {
            JobQueueEntryState.Pending => JobState.Pending,
            JobQueueEntryState.Claimed => JobState.Running,
            JobQueueEntryState.Completed => JobState.Completed,
            JobQueueEntryState.Failed => JobState.Failed,
            _ => JobState.Pending,
        };

    private async Task CreateJobProgressAsync(
        Guid id,
        Type jobType,
        string? data,
        CancellationToken ct
    )
    {
        var moduleName =
            jobType.Assembly.GetName().Name?.Replace("SimpleModule.", "", StringComparison.Ordinal)
            ?? UnknownValue;
        db.JobProgress.Add(
            new JobProgress
            {
                Id = JobId.From(id),
                JobTypeName = jobType.AssemblyQualifiedName!,
                ModuleName = moduleName,
                ProgressPercentage = 0,
                Data = data,
                UpdatedAt = DateTimeOffset.UtcNow,
            }
        );
        await db.SaveChangesAsync(ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {JobType} enqueued ({JobId})")]
    private static partial void LogJobEnqueued(ILogger logger, string jobType, Guid jobId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Job {JobType} scheduled ({JobId}) for {ExecuteAt}"
    )]
    private static partial void LogJobScheduled(
        ILogger logger,
        string jobType,
        Guid jobId,
        DateTimeOffset executeAt
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Recurring job '{Name}' added with cron '{CronExpression}'"
    )]
    private static partial void LogRecurringJobAdded(
        ILogger logger,
        string name,
        string cronExpression
    );
}
