using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed partial class AuditWriterService(
    AuditChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditWriterService> logger
) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);
        var batch = new List<AuditEntry>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    batch.Clear();
                    var deadline = DateTimeOffset.UtcNow.Add(FlushInterval);

                    while (
                        batch.Count < BatchSize
                        && DateTimeOffset.UtcNow < deadline
                        && channel.Reader.TryRead(out var entry)
                    )
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count > 0)
                    {
                        await FlushBatchAsync(batch, stoppingToken);
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
            await FlushBatchAsync(batch, CancellationToken.None);
        }

        LogStopped(logger);
    }

    private async Task FlushBatchAsync(List<AuditEntry> batch, CancellationToken ct)
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
