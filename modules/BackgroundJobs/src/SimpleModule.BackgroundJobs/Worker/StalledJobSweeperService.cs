// modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Worker/StalledJobSweeperService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.Worker;

public sealed partial class StalledJobSweeperService(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobsWorkerOptions> options,
    ILogger<StalledJobSweeperService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(opts.StallSweepInterval, stoppingToken);
                await using var scope = scopeFactory.CreateAsyncScope();
                var queue = scope.ServiceProvider.GetRequiredService<IJobQueue>();
                var count = await queue.RequeueStalledAsync(opts.StallTimeout, stoppingToken);
                if (count > 0) LogSwept(logger, count);
            }
            catch (OperationCanceledException) { break; }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogError(logger, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stall sweeper requeued {Count} job(s)")]
    private static partial void LogSwept(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stall sweeper error")]
    private static partial void LogError(ILogger logger, Exception ex);
}
