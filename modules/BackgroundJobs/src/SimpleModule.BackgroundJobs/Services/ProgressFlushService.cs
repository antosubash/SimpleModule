using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Entities;

namespace SimpleModule.BackgroundJobs.Services;

public sealed partial class ProgressFlushService(
    ProgressChannel channel,
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobsModuleOptions> moduleOptions,
    ILogger<ProgressFlushService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);
        var opts = moduleOptions.Value;
        var batch = new List<ProgressEntry>(opts.ProgressFlushBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    batch.Clear();
                    var deadline = DateTimeOffset.UtcNow.Add(opts.ProgressFlushInterval);

                    while (
                        batch.Count < opts.ProgressFlushBatchSize
                        && DateTimeOffset.UtcNow < deadline
                        && channel.Reader.TryRead(out var entry)
                    )
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count > 0)
                    {
                        // Use CancellationToken.None: entries were already consumed from the
                        // channel, so the flush must complete to avoid data loss if StopAsync
                        // cancels stoppingToken mid-save.
                        await FlushBatchAsync(batch, opts.MaxLogEntries, CancellationToken.None);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogFlushError(logger, ex);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        // Drain remaining on shutdown
        batch.Clear();
        while (channel.Reader.TryRead(out var entry))
        {
            batch.Add(entry);
        }
        if (batch.Count > 0)
        {
            await FlushBatchAsync(batch, moduleOptions.Value.MaxLogEntries, CancellationToken.None);
        }

        LogStopped(logger);
    }

    private async Task FlushBatchAsync(
        List<ProgressEntry> batch,
        int maxLogEntries,
        CancellationToken ct
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BackgroundJobsDbContext>();

        // Group by JobId — take latest progress per job
        var grouped = batch
            .GroupBy(e => e.JobId)
            .Select(g => new
            {
                JobId = g.Key,
                LatestProgress = g.Where(e => e.Message is not null).MaxBy(e => e.Timestamp),
                LogEntries = g.Where(e => e.LogMessage is not null)
                    .Select(e => new JobLogEntry
                    {
                        Message = e.LogMessage!,
                        Timestamp = e.Timestamp,
                    })
                    .ToList(),
            })
            .ToList();

        var jobIds = grouped.Select(g => g.JobId).ToList();
        var existingMap = await db
            .JobProgress.Where(p => jobIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var group in grouped)
        {
            if (!existingMap.TryGetValue(group.JobId, out var existing))
            {
                continue;
            }

            if (group.LatestProgress is not null)
            {
                existing.ProgressPercentage = group.LatestProgress.Percentage;
                existing.ProgressMessage = group.LatestProgress.Message;
            }

            if (group.LogEntries.Count > 0)
            {
                var logs = string.IsNullOrEmpty(existing.Logs)
                    ? []
                    : JsonSerializer.Deserialize<List<JobLogEntry>>(existing.Logs) ?? [];

                logs.AddRange(group.LogEntries);

                // Cap at max entries
                if (logs.Count > maxLogEntries)
                {
                    logs = logs.Skip(logs.Count - maxLogEntries).ToList();
                }

                existing.Logs = JsonSerializer.Serialize(logs);
            }

            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        LogFlushed(logger, batch.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "ProgressFlushService started")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "ProgressFlushService stopped")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Flushed {Count} progress entries")]
    private static partial void LogFlushed(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error flushing progress entries")]
    private static partial void LogFlushError(ILogger logger, Exception ex);
}
