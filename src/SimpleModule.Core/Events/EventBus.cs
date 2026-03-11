using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Core.Events;

public sealed partial class EventBus(
    IServiceProvider serviceProvider,
    ILogger<EventBus> logger
) : IEventBus
{
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<T>>();
        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken);
            }
#pragma warning disable CA1031 // Event bus must isolate handler failures
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogHandlerFailed(logger, handler.GetType().Name, typeof(T).Name, ex);
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(
                $"One or more event handlers for {typeof(T).Name} failed.",
                exceptions
            );
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Event handler {HandlerName} failed for event {EventName}"
    )]
    private static partial void LogHandlerFailed(
        ILogger logger,
        string handlerName,
        string eventName,
        Exception exception
    );
}
