// modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Services/BackgroundJobsContractsService.cs
using System.Text.Json;
using Cronos;
using Microsoft.EntityFrameworkCore;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using static SimpleModule.BackgroundJobs.BackgroundJobsInternalConstants;

namespace SimpleModule.BackgroundJobs.Services;

public sealed class BackgroundJobsContractsService(IJobQueue queue, BackgroundJobsDbContext db)
    : IBackgroundJobsContracts
{
    public async Task<PagedResult<JobSummaryDto>> GetJobsAsync(
        JobFilter filter,
        CancellationToken ct
    )
    {
        var query = db.JobQueueEntries.AsNoTracking().Where(e => e.RecurringName == null);
        if (filter.State.HasValue)
        {
            var states = MapJobStateToQueueStates(filter.State.Value);
            query = query.Where(e => states.Contains(e.State));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        var ids = rows.Select(r => r.Id).ToList();
        var progressMap = await db
            .JobProgress.AsNoTracking()
            .Where(j => ids.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, ct);

        var items = rows.Select(r =>
            {
                progressMap.TryGetValue(r.Id, out var p);
                return new JobSummaryDto
                {
                    Id = r.Id,
                    JobType = GetShortTypeName(r.JobTypeName),
                    State = BackgroundJobsService.MapQueueState(r.State),
                    ProgressPercentage =
                        p?.ProgressPercentage
                        ?? (r.State == JobQueueEntryState.Completed ? 100 : 0),
                    ProgressMessage = p?.ProgressMessage,
                    CreatedAt = r.CreatedAt,
                    CompletedAt = r.CompletedAt,
                };
            })
            .ToList();

        return new PagedResult<JobSummaryDto>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<JobDetailDto?> GetJobDetailAsync(JobId id, CancellationToken ct)
    {
        var row = await db.JobQueueEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (row is null)
            return null;

        var progress = await db.JobProgress.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);
        var logs = !string.IsNullOrEmpty(progress?.Logs)
            ? JsonSerializer.Deserialize<List<JobLogEntry>>(progress.Logs) ?? []
            : [];

        return new JobDetailDto
        {
            Id = id,
            JobType = GetShortTypeName(row.JobTypeName),
            ModuleName = GetModuleName(row.JobTypeName),
            State = BackgroundJobsService.MapQueueState(row.State),
            ProgressPercentage =
                progress?.ProgressPercentage
                ?? (row.State == JobQueueEntryState.Completed ? 100 : 0),
            ProgressMessage = progress?.ProgressMessage,
            Error = row.Error,
            Data = progress?.Data,
            Logs = logs,
            RetryCount = Math.Max(0, row.AttemptCount - 1),
            CreatedAt = row.CreatedAt,
            StartedAt = row.ClaimedAt,
            CompletedAt = row.CompletedAt,
        };
    }

    public async Task<IReadOnlyList<RecurringJobDto>> GetRecurringJobsAsync(CancellationToken ct)
    {
        var rows = await db
            .JobQueueEntries.AsNoTracking()
            .Where(e => e.RecurringName != null)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var disabledSentinel = DateTimeOffset.MaxValue.AddYears(-1);

        return rows.Select(r =>
            {
                DateTimeOffset? next = null;
                var isEnabled = r.ScheduledAt < disabledSentinel;
                if (isEnabled && r.CronExpression is not null)
                {
                    try
                    {
                        var format =
                            r.CronExpression.Split(' ').Length > 5
                                ? CronFormat.IncludeSeconds
                                : CronFormat.Standard;
                        var cron = CronExpression.Parse(r.CronExpression, format);
                        var n = cron.GetNextOccurrence(now, inclusive: false);
                        if (n.HasValue)
                            next = new DateTimeOffset(n.Value, TimeSpan.Zero);
                    }
                    catch (CronFormatException) { }
                }

                return new RecurringJobDto
                {
                    Id = RecurringJobId.From(r.Id.Value),
                    Name = r.RecurringName ?? UnknownValue,
                    JobType = GetShortTypeName(r.JobTypeName),
                    CronExpression = r.CronExpression ?? string.Empty,
                    IsEnabled = isEnabled,
                    LastRunAt = null,
                    NextRunAt = isEnabled ? next : null,
                    CreatedAt = r.CreatedAt,
                };
            })
            .ToList();
    }

    public async Task<int> GetRecurringCountAsync(CancellationToken ct) =>
        await db.JobQueueEntries.AsNoTracking().CountAsync(e => e.RecurringName != null, ct);

    public async Task RetryAsync(JobId id, CancellationToken ct)
    {
        var row =
            await db.JobQueueEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new InvalidOperationException($"Job {id} not found.");

        var newId = Guid.NewGuid();
        await queue.EnqueueAsync(
            new JobQueueEntry(
                newId,
                row.JobTypeName,
                row.SerializedData,
                DateTimeOffset.UtcNow,
                JobQueueEntryState.Pending,
                0,
                null,
                null,
                DateTimeOffset.UtcNow
            ),
            ct
        );

        db.JobProgress.Add(
            new JobProgress
            {
                Id = JobId.From(newId),
                JobTypeName = row.JobTypeName,
                ModuleName = GetModuleName(row.JobTypeName),
                ProgressPercentage = 0,
                Data = row.SerializedData,
                UpdatedAt = DateTimeOffset.UtcNow,
            }
        );
        await db.SaveChangesAsync(ct);
    }

    private static JobQueueEntryState[] MapJobStateToQueueStates(JobState state) =>
        state switch
        {
            JobState.Pending => [JobQueueEntryState.Pending],
            JobState.Running => [JobQueueEntryState.Claimed],
            JobState.Completed => [JobQueueEntryState.Completed],
            JobState.Failed => [JobQueueEntryState.Failed],
            _ => [],
        };
}
