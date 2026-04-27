using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed partial class AuditWriterService(
    AuditChannel channel,
    IServiceScopeFactory scopeFactory,
    IOptions<AuditLogsModuleOptions> moduleOptions,
    ILogger<AuditWriterService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);
        var opts = moduleOptions.Value;
        var batch = new List<AuditEntry>(opts.WriterBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    break;
                }

                batch.Clear();

                while (batch.Count < opts.WriterBatchSize && channel.Reader.TryRead(out var entry))
                {
                    batch.Add(entry);
                }

                if (batch.Count < opts.WriterBatchSize)
                {
                    using var linger = CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken
                    );
                    linger.CancelAfter(opts.WriterFlushInterval);

                    try
                    {
                        while (batch.Count < opts.WriterBatchSize)
                        {
                            if (!await channel.Reader.WaitToReadAsync(linger.Token))
                            {
                                break;
                            }

                            while (
                                batch.Count < opts.WriterBatchSize
                                && channel.Reader.TryRead(out var entry)
                            )
                            {
                                batch.Add(entry);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Linger deadline OR shutdown — fall through and persist
                        // whatever was already drained instead of dropping it.
                    }
                }

                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch);
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
            await FlushBatchAsync(batch);
        }

        LogStopped(logger);
    }

    private async Task FlushBatchAsync(List<AuditEntry> batch)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var contracts = scope.ServiceProvider.GetRequiredService<IAuditLogContracts>();
        await contracts.WriteBatchAsync(batch);
        LogFlushed(logger, batch.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "AuditWriterService started")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "AuditWriterService stopped")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Flushed {Count} audit entries")]
    private static partial void LogFlushed(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error flushing audit entries")]
    private static partial void LogFlushError(ILogger logger, Exception ex);
}
