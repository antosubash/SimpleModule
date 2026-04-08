// modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Worker/JobProcessorService.cs
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;

namespace SimpleModule.BackgroundJobs.Worker;

public sealed partial class JobProcessorService(
    IServiceScopeFactory scopeFactory,
    JobTypeRegistry registry,
    WorkerIdentity identity,
    IOptions<BackgroundJobsWorkerOptions> options,
    ILogger<JobProcessorService> logger
) : BackgroundService
{
    private readonly BackgroundJobsWorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, identity.Id, _options.MaxConcurrency);
        using var semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await semaphore.WaitAsync(stoppingToken);

                JobQueueEntry? entry;
                await using (var dequeueScope = scopeFactory.CreateAsyncScope())
                {
                    var queue = dequeueScope.ServiceProvider.GetRequiredService<IJobQueue>();
                    entry = await queue.DequeueAsync(identity.Id, stoppingToken);
                }

                if (entry is null)
                {
                    semaphore.Release();
                    await Task.Delay(_options.PollInterval, stoppingToken);
                    continue;
                }

                _ = Task.Run(async () =>
                {
                    try { await ExecuteEntryAsync(entry, stoppingToken); }
                    finally { semaphore.Release(); }
                }, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogLoopError(logger, ex);
                await Task.Delay(_options.PollInterval, CancellationToken.None);
            }
        }
    }

    private async Task ExecuteEntryAsync(JobQueueEntry entry, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<IJobQueue>();

        var jobType = registry.Resolve(entry.JobTypeName);
        if (jobType is null)
        {
            LogUnknownType(logger, entry.Id, entry.JobTypeName);
            await queue.FailAsync(entry.Id, $"Unknown job type: {entry.JobTypeName}", ct);
            return;
        }

        var progressChannel = scope.ServiceProvider.GetRequiredService<ProgressChannel>();
        var jobInstance = (IModuleJob)scope.ServiceProvider.GetRequiredService(jobType);
        var context = new DefaultJobExecutionContext(JobId.From(entry.Id), entry.SerializedData, progressChannel);

        try
        {
            LogExecuting(logger, entry.Id, jobType.Name);
            await jobInstance.ExecuteAsync(context, ct);
            await queue.CompleteAsync(entry.Id, ct);
            LogCompleted(logger, entry.Id, jobType.Name);

            if (entry.CronExpression is not null && entry.RecurringName is not null)
            {
                await ScheduleNextRecurringAsync(queue, entry, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown — leave as Claimed, stall sweeper will requeue.
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogJobError(logger, entry.Id, jobType.Name, ex);
            if (entry.AttemptCount < _options.MaxAttempts)
            {
                var delay = TimeSpan.FromSeconds(_options.RetryBaseDelay.TotalSeconds * Math.Pow(2, entry.AttemptCount - 1));
                var retry = new JobQueueEntry(
                    Guid.NewGuid(), entry.JobTypeName, entry.SerializedData,
                    DateTimeOffset.UtcNow + delay, JobQueueEntryState.Pending,
                    entry.AttemptCount, null, null, DateTimeOffset.UtcNow);
                await queue.EnqueueAsync(retry, ct);
                await queue.FailAsync(entry.Id, $"{ex.Message} (retry scheduled)", ct);
            }
            else
            {
                await queue.FailAsync(entry.Id, ex.Message, ct);
            }
        }
    }

    private static async Task ScheduleNextRecurringAsync(IJobQueue queue, JobQueueEntry entry, CancellationToken ct)
    {
        var format = entry.CronExpression!.Split(' ').Length > 5 ? CronFormat.IncludeSeconds : CronFormat.Standard;
        var cron = CronExpression.Parse(entry.CronExpression, format);
        var next = cron.GetNextOccurrence(DateTime.UtcNow, inclusive: false);
        if (!next.HasValue) return;

        await queue.EnqueueAsync(new JobQueueEntry(
            Guid.NewGuid(), entry.JobTypeName, entry.SerializedData,
            new DateTimeOffset(next.Value, TimeSpan.Zero),
            JobQueueEntryState.Pending, 0, entry.CronExpression, entry.RecurringName,
            DateTimeOffset.UtcNow), ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker {WorkerId} started with concurrency {Concurrency}")]
    private static partial void LogStarted(ILogger logger, string workerId, int concurrency);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing job {EntryId} ({JobType})")]
    private static partial void LogExecuting(ILogger logger, Guid entryId, string jobType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {EntryId} ({JobType}) completed")]
    private static partial void LogCompleted(ILogger logger, Guid entryId, string jobType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown job type for entry {EntryId}: {TypeName}")]
    private static partial void LogUnknownType(ILogger logger, Guid entryId, string typeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Job {EntryId} ({JobType}) threw an exception")]
    private static partial void LogJobError(ILogger logger, Guid entryId, string jobType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Processor loop error")]
    private static partial void LogLoopError(ILogger logger, Exception ex);
}
