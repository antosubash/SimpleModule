using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Core.Events;

/// <summary>
/// Background service that drains the <see cref="BackgroundEventChannel"/> and dispatches
/// events to their handlers in a scoped DI context. Events are dispatched concurrently
/// to avoid head-of-line blocking from slow handlers.
/// </summary>
public sealed partial class BackgroundEventDispatcher(
    BackgroundEventChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundEventDispatcher> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var dispatch in channel.Reader.ReadAllAsync(stoppingToken))
        {
            _ = DispatchAsync(dispatch, stoppingToken);
        }
    }

    private async Task DispatchAsync(
        Func<IServiceProvider, CancellationToken, Task> dispatch,
        CancellationToken stoppingToken
    )
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await dispatch(scope.ServiceProvider, stoppingToken);
        }
#pragma warning disable CA1031 // Background dispatcher must not crash on handler failures
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogDispatchFailed(logger, ex);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Background event dispatch failed"
    )]
    private static partial void LogDispatchFailed(ILogger logger, Exception exception);
}
