using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Entities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;
using static SimpleModule.BackgroundJobs.BackgroundJobsConstants;

namespace SimpleModule.BackgroundJobs.Services;

public sealed partial class BackgroundJobsService(
    ITimeTickerManager<TimeTickerEntity> timeManager,
    ICronTickerManager<CronTickerEntity> cronManager,
    BackgroundJobsDbContext db,
    JobTypeRegistry registry,
    ILogger<BackgroundJobsService> logger
) : IBackgroundJobs
{
    public async Task<JobId> EnqueueAsync<TJob>(object? data, CancellationToken ct)
        where TJob : IModuleJob
    {
        var jobType = typeof(TJob);
        var payload = CreatePayload(jobType, data);

        var ticker = new TimeTickerEntity
        {
            Function = DispatcherFunctionName,
            ExecutionTime = DateTime.UtcNow,
            Request = JsonSerializer.SerializeToUtf8Bytes(payload),
        };

        await timeManager.AddAsync(ticker);

        await CreateJobProgressAsync(ticker.Id, jobType, data, ct);

        LogJobEnqueued(logger, jobType.Name, ticker.Id);
        return JobId.From(ticker.Id);
    }

    public async Task<JobId> ScheduleAsync<TJob>(
        DateTimeOffset executeAt,
        object? data,
        CancellationToken ct
    )
        where TJob : IModuleJob
    {
        var jobType = typeof(TJob);
        var payload = CreatePayload(jobType, data);

        var ticker = new TimeTickerEntity
        {
            Function = DispatcherFunctionName,
            ExecutionTime = executeAt.UtcDateTime,
            Request = JsonSerializer.SerializeToUtf8Bytes(payload),
        };

        await timeManager.AddAsync(ticker);

        await CreateJobProgressAsync(ticker.Id, jobType, data, ct);

        LogJobScheduled(logger, jobType.Name, ticker.Id, executeAt);
        return JobId.From(ticker.Id);
    }

    public async Task<RecurringJobId> AddRecurringAsync<TJob>(
        string name,
        string cronExpression,
        object? data,
        CancellationToken ct
    )
        where TJob : IModuleJob
    {
        var jobType = typeof(TJob);
        var payload = CreatePayload(jobType, data);

        var ticker = new CronTickerEntity
        {
            Function = DispatcherFunctionName,
            Description = name,
            Expression = cronExpression,
            Request = JsonSerializer.SerializeToUtf8Bytes(payload),
            IsEnabled = true,
        };

        await cronManager.AddAsync(ticker);

        LogRecurringJobAdded(logger, name, cronExpression);
        return RecurringJobId.From(ticker.Id);
    }

    public async Task RemoveRecurringAsync(RecurringJobId id, CancellationToken ct)
    {
        await cronManager.DeleteAsync(id.Value);
    }

    public async Task<bool> ToggleRecurringAsync(RecurringJobId id, CancellationToken ct)
    {
        var ticker = await db.CronTickers
            .FirstOrDefaultAsync(c => c.Id == id.Value, ct)
            ?? throw new InvalidOperationException($"Recurring job {id} not found.");

        ticker.IsEnabled = !ticker.IsEnabled;
        await db.SaveChangesAsync(ct);
        return ticker.IsEnabled;
    }

    public async Task CancelAsync(JobId jobId, CancellationToken ct)
    {
        await timeManager.DeleteAsync(jobId.Value);
    }

    public async Task<JobStatusDto?> GetStatusAsync(JobId jobId, CancellationToken ct)
    {
        var ticker = await db.TimeTickers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == jobId.Value, ct);

        if (ticker is null)
        {
            return null;
        }

        var progress = await db.JobProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId.Value, ct);

        return new JobStatusDto(
            Id: jobId,
            JobType: GetShortTypeName(progress?.JobTypeName),
            State: MapTickerStatus(ticker.Status),
            ProgressPercentage: progress?.ProgressPercentage
                ?? (ticker.Status == TickerStatus.Done ? 100 : 0),
            ProgressMessage: progress?.ProgressMessage,
            Error: ticker.ExceptionMessage,
            CreatedAt: AsUtc(ticker.CreatedAt),
            StartedAt: ticker.ExecutedAt.HasValue
                ? AsUtc(ticker.ExecutedAt.Value)
                : null,
            CompletedAt: ticker.Status is TickerStatus.Done or TickerStatus.DueDone or TickerStatus.Failed
                ? AsUtc(ticker.UpdatedAt)
                : null,
            RetryCount: ticker.RetryCount
        );
    }

    private async Task CreateJobProgressAsync(
        Guid tickerId,
        Type jobType,
        object? data,
        CancellationToken ct
    )
    {
        var moduleName = jobType.Assembly.GetName().Name?.Replace("SimpleModule.", "") ?? UnknownValue;

        db.JobProgress.Add(
            new JobProgress
            {
                Id = tickerId,
                JobTypeName = jobType.AssemblyQualifiedName!,
                ModuleName = moduleName,
                ProgressPercentage = 0,
                Data = data is not null ? JsonSerializer.Serialize(data) : null,
                UpdatedAt = DateTimeOffset.UtcNow,
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static JobDispatchPayload CreatePayload(Type jobType, object? data)
    {
        return new JobDispatchPayload(
            jobType.AssemblyQualifiedName!,
            data is not null ? JsonSerializer.Serialize(data) : null
        );
    }

    internal static JobState MapTickerStatus(TickerStatus status)
    {
        return status switch
        {
            TickerStatus.Idle or TickerStatus.Queued => JobState.Pending,
            TickerStatus.InProgress => JobState.Running,
            TickerStatus.Done or TickerStatus.DueDone => JobState.Completed,
            TickerStatus.Failed => JobState.Failed,
            TickerStatus.Cancelled => JobState.Cancelled,
            TickerStatus.Skipped => JobState.Skipped,
            _ => JobState.Pending,
        };
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
