using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Core.Events;

/// <summary>
/// Background service that drains the <see cref="BackgroundEventChannel"/> and dispatches
/// events to their handlers in a scoped DI context.
/// </summary>
public sealed partial class BackgroundEventDispatcher(
    BackgroundEventChannel channel,
    IServiceProvider serviceProvider,
    ILogger<BackgroundEventDispatcher> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var dispatch in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                await dispatch(scope.ServiceProvider, stoppingToken);
            }
#pragma warning disable CA1031 // Background dispatcher must not crash on handler failures
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogDispatchFailed(logger, ex);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Background event dispatch failed"
    )]
    private static partial void LogDispatchFailed(ILogger logger, Exception exception);
}
