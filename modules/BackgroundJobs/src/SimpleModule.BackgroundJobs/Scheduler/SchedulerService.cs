using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Worker;

namespace SimpleModule.BackgroundJobs.Scheduler;

/// <summary>
/// Hosted service that turns code-declared scheduled jobs into enqueued
/// <see cref="JobQueueEntry"/>s. Ticks every <see cref="SchedulerOptions.TickInterval"/>:
/// reconciles in-memory definitions to the database, optionally acquires the
/// <c>OnOneServer</c> lease, then enqueues each due definition (respecting
/// the per-job <c>WithoutOverlapping</c> mutex).
/// </summary>
internal sealed partial class SchedulerService(
    IServiceScopeFactory scopeFactory,
    IScheduler registry,
    WorkerIdentity identity,
    IOptions<SchedulerOptions> options,
    TimeProvider clock,
    ILogger<SchedulerService> logger
) : BackgroundService
{
    private readonly SchedulerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, identity.Id, _options.TickInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogTickError(logger, ex);
            }

            try
            {
                await Task.Delay(_options.TickInterval, clock, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task TickOnceAsync(CancellationToken ct)
    {
        var definitions = registry.Definitions;
        if (definitions.Count == 0)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var now = clock.GetUtcNow();

        var db = sp.GetRequiredService<BackgroundJobsDbContext>();
        await SchedulerReconciler.ReconcileAsync(db, definitions, now, logger, ct);

        // Only acquire a lease if at least one registered definition asked for it.
        if (definitions.Any(d => d.OnOneServer))
        {
            var leader = sp.GetRequiredService<IInstanceLeader>();
            var heldByMe = await leader.TryAcquireAsync(
                SchedulerOptions.LeaseName,
                identity.Id,
                _options.LeaseTtl,
                ct
            );
            if (!heldByMe)
            {
                LogLeaseLost(logger, identity.Id);
                return;
            }
        }

        var dueNames = await db
            .ScheduledJobStates.Where(s =>
                s.IsEnabled && s.NextRunAt != null && s.NextRunAt <= now
            )
            .Select(s => s.Name)
            .ToListAsync(ct);

        if (dueNames.Count == 0)
            return;

        var byName = definitions.ToDictionary(d => d.Name, StringComparer.Ordinal);

        foreach (var name in dueNames)
        {
            if (!byName.TryGetValue(name, out var def))
            {
                // Definition was removed in code but row remains — skip silently.
                continue;
            }

            try
            {
                await ProcessOneAsync(name, def, now, ct);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogDefinitionError(logger, name, ex);
            }
        }
    }

    // Fresh scope per due definition so mutex/queue saves never flush other
    // tracked state, and so a failure in one definition can't leave the
    // outer DbContext with partially-modified rows.
    private async Task ProcessOneAsync(
        string name,
        ScheduledJobDefinition def,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<BackgroundJobsDbContext>();
        var state = await db.ScheduledJobStates.FirstOrDefaultAsync(s => s.Name == name, ct);
        if (state is null)
            return;

        if (state.WithoutOverlapping)
        {
            var mutex = sp.GetRequiredService<IJobMutex>();
            var acquired = await mutex.TryAcquireAsync(
                MutexNameFor(state.Name),
                identity.Id,
                _options.MutexTtl,
                ct
            );
            if (!acquired)
            {
                // Skip this tick but advance NextRunAt so we don't busy-loop on a stuck mutex.
                LogMutexSkip(logger, state.Name);
                state.NextRunAt = CronCalculator.GetNextOccurrence(
                    state.CronExpression,
                    state.TimeZoneId,
                    now
                );
                state.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                return;
            }
        }

        var jobId = Guid.NewGuid();
        var queue = sp.GetRequiredService<IJobQueue>();
        await queue.EnqueueAsync(
            new JobQueueEntry(
                jobId,
                state.JobTypeName,
                state.Payload,
                now,
                JobQueueEntryState.Pending,
                0,
                state.CronExpression,
                SchedulerOptions.ScheduledJobSentinel + state.Name,
                now
            ),
            ct
        );

        state.LastRunAt = now;
        state.NextRunAt = CronCalculator.GetNextOccurrence(
            state.CronExpression,
            state.TimeZoneId,
            now
        );
        state.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        LogEnqueued(logger, state.Name, jobId, state.NextRunAt);
    }

    internal static string MutexNameFor(string scheduledJobName) =>
        SchedulerOptions.MutexPrefix + scheduledJobName;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scheduler started on {WorkerId} (tick={Tick})"
    )]
    private static partial void LogStarted(ILogger logger, string workerId, TimeSpan tick);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduler tick on {WorkerId} did not hold lease — skipping")]
    private static partial void LogLeaseLost(ILogger logger, string workerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Enqueued scheduled job '{Name}' as {JobId}; next run {NextRunAt}"
    )]
    private static partial void LogEnqueued(
        ILogger logger,
        string name,
        Guid jobId,
        DateTimeOffset? nextRunAt
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scheduled job '{Name}' skipped — mutex held"
    )]
    private static partial void LogMutexSkip(ILogger logger, string name);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Scheduler error processing definition '{Name}'"
    )]
    private static partial void LogDefinitionError(ILogger logger, string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler tick error")]
    private static partial void LogTickError(ILogger logger, Exception ex);
}
