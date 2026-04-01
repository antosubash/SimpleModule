using System.Text.Json;
using Cronos;
using Microsoft.EntityFrameworkCore;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;
using static SimpleModule.BackgroundJobs.BackgroundJobsConstants;

namespace SimpleModule.BackgroundJobs.Services;

public sealed class BackgroundJobsContractsService(
    BackgroundJobsDbContext db,
    ITimeTickerManager<TimeTickerEntity> timeManager
) : IBackgroundJobsContracts
{
    public async Task<PagedResult<JobSummaryDto>> GetJobsAsync(
        JobFilter filter,
        CancellationToken ct
    )
    {
        var query = db.TimeTickers.AsNoTracking().AsQueryable();

        if (filter.State.HasValue)
        {
            var tickerStatuses = GetTickerStatusesForJobState(filter.State.Value);
            query = query.Where(t => tickerStatuses.Contains(t.Status));
        }

        var totalCount = await query.CountAsync(ct);

        var tickers = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        var tickerIds = tickers.Select(t => t.Id).ToList();
        var progressMap = await db.JobProgress
            .AsNoTracking()
            .Where(j => tickerIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, ct);

        var items = tickers
            .Select(t =>
            {
                progressMap.TryGetValue(t.Id, out var progress);
                return new JobSummaryDto
                {
                    Id = JobId.From(t.Id),
                    JobType = GetShortTypeName(progress?.JobTypeName),
                    State = BackgroundJobsService.MapTickerStatus(t.Status),
                    ProgressPercentage = progress?.ProgressPercentage
                        ?? (t.Status == TickerQ.Utilities.Enums.TickerStatus.Done ? 100 : 0),
                    ProgressMessage = progress?.ProgressMessage,
                    CreatedAt = AsUtc(t.CreatedAt),
                    CompletedAt = t.Status is TickerQ.Utilities.Enums.TickerStatus.Done
                            or TickerQ.Utilities.Enums.TickerStatus.DueDone
                            or TickerQ.Utilities.Enums.TickerStatus.Failed
                        ? AsUtc(t.UpdatedAt)
                        : null,
                };
            })
            .ToList();

        return new PagedResult<JobSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<JobDetailDto?> GetJobDetailAsync(JobId id, CancellationToken ct)
    {
        var ticker = await db.TimeTickers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id.Value, ct);

        if (ticker is null)
        {
            return null;
        }

        var progress = await db.JobProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id.Value, ct);

        var logs = !string.IsNullOrEmpty(progress?.Logs)
            ? JsonSerializer.Deserialize<List<JobLogEntry>>(progress.Logs) ?? []
            : [];

        return new JobDetailDto
        {
            Id = id,
            JobType = GetShortTypeName(progress?.JobTypeName),
            ModuleName = progress?.ModuleName ?? UnknownValue,
            State = BackgroundJobsService.MapTickerStatus(ticker.Status),
            ProgressPercentage = progress?.ProgressPercentage
                ?? (ticker.Status == TickerQ.Utilities.Enums.TickerStatus.Done ? 100 : 0),
            ProgressMessage = progress?.ProgressMessage,
            Error = ticker.ExceptionMessage,
            Data = progress?.Data,
            Logs = logs,
            RetryCount = ticker.RetryCount,
            CreatedAt = AsUtc(ticker.CreatedAt),
            StartedAt = ticker.ExecutedAt.HasValue
                ? AsUtc(ticker.ExecutedAt.Value)
                : null,
            CompletedAt = ticker.Status is TickerQ.Utilities.Enums.TickerStatus.Done
                    or TickerQ.Utilities.Enums.TickerStatus.DueDone
                    or TickerQ.Utilities.Enums.TickerStatus.Failed
                ? AsUtc(ticker.UpdatedAt)
                : null,
        };
    }

    public async Task<IReadOnlyList<RecurringJobDto>> GetRecurringJobsAsync(CancellationToken ct)
    {
        var cronTickers = await db.CronTickers
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var cronTickerIds = cronTickers.Select(c => c.Id).ToList();

        var lastRunMap = await db.CronTickerOccurrences
            .AsNoTracking()
            .Where(o => cronTickerIds.Contains(o.CronTickerId) && o.ExecutedAt != null)
            .GroupBy(o => o.CronTickerId)
            .Select(g => new { CronTickerId = g.Key, LastRunAt = g.Max(o => o.ExecutedAt) })
            .ToDictionaryAsync(x => x.CronTickerId, x => x.LastRunAt, ct);

        var now = DateTime.UtcNow;

        return cronTickers
            .Select(c =>
            {
                lastRunMap.TryGetValue(c.Id, out var lastRunAt);

                DateTimeOffset? nextRunAt = null;
                try
                {
                    var format = c.Expression.Split(' ').Length > 5
                        ? CronFormat.IncludeSeconds
                        : CronFormat.Standard;
                    var cronExpression = CronExpression.Parse(c.Expression, format);
                    var next = cronExpression.GetNextOccurrence(now, inclusive: false);
                    if (next.HasValue)
                    {
                        nextRunAt = AsUtc(next.Value);
                    }
                }
                catch (CronFormatException)
                {
                    // Invalid expression stored in DB — leave NextRunAt null
                }

                return new RecurringJobDto
                {
                    Id = RecurringJobId.From(c.Id),
                    Name = c.Description,
                    JobType = ExtractJobTypeFromRequest(c.Request),
                    CronExpression = c.Expression,
                    IsEnabled = c.IsEnabled,
                    LastRunAt = lastRunAt.HasValue ? AsUtc(lastRunAt.Value) : null,
                    NextRunAt = c.IsEnabled ? nextRunAt : null,
                    CreatedAt = AsUtc(c.CreatedAt),
                };
            })
            .ToList();
    }

    public async Task<int> GetRecurringCountAsync(CancellationToken ct)
    {
        return await db.CronTickers.AsNoTracking().CountAsync(ct);
    }

    public async Task RetryAsync(JobId id, CancellationToken ct)
    {
        var ticker = await db.TimeTickers
            .FirstOrDefaultAsync(t => t.Id == id.Value, ct)
            ?? throw new InvalidOperationException($"Job {id} not found.");

        var payload = JsonSerializer.Deserialize<JobDispatchPayload>(ticker.Request)
            ?? throw new InvalidOperationException($"Job {id} has no payload.");

        var newTicker = new TimeTickerEntity
        {
            Function = DispatcherFunctionName,
            ExecutionTime = DateTime.UtcNow,
            Request = ticker.Request,
        };

#pragma warning disable CA2016 // TickerQ manager methods do not accept CancellationToken
        await timeManager.AddAsync(newTicker);
#pragma warning restore CA2016

        db.JobProgress.Add(
            new Entities.JobProgress
            {
                Id = newTicker.Id,
                JobTypeName = payload.JobTypeName,
                ModuleName = GetModuleName(payload.JobTypeName),
                ProgressPercentage = 0,
                Data = payload.SerializedData,
                UpdatedAt = DateTimeOffset.UtcNow,
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static string ExtractJobTypeFromRequest(byte[] request)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JobDispatchPayload>(request);
            return GetShortTypeName(payload?.JobTypeName);
        }
        catch (JsonException)
        {
            return UnknownValue;
        }
    }

    private static List<TickerQ.Utilities.Enums.TickerStatus> GetTickerStatusesForJobState(
        JobState state
    )
    {
        return state switch
        {
            JobState.Pending => [TickerQ.Utilities.Enums.TickerStatus.Idle, TickerQ.Utilities.Enums.TickerStatus.Queued],
            JobState.Running => [TickerQ.Utilities.Enums.TickerStatus.InProgress],
            JobState.Completed => [TickerQ.Utilities.Enums.TickerStatus.Done, TickerQ.Utilities.Enums.TickerStatus.DueDone],
            JobState.Failed => [TickerQ.Utilities.Enums.TickerStatus.Failed],
            JobState.Cancelled => [TickerQ.Utilities.Enums.TickerStatus.Cancelled],
            JobState.Skipped => [TickerQ.Utilities.Enums.TickerStatus.Skipped],
            _ => [],
        };
    }
}
