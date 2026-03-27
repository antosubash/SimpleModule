using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Core.Events;

/// <summary>
/// Unbounded channel for queuing events to be dispatched by a background service.
/// Register as a singleton. The <see cref="BackgroundEventDispatcher"/> reads from this channel.
/// </summary>
public sealed partial class BackgroundEventChannel(ILogger<BackgroundEventChannel> logger)
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>(
            new UnboundedChannelOptions { SingleReader = true }
        );

    internal ChannelReader<Func<IServiceProvider, CancellationToken, Task>> Reader =>
        _channel.Reader;

    internal void Enqueue<T>(T @event)
        where T : IEvent
    {
        // Capture the event in a closure that will be executed by the background dispatcher.
        // The dispatcher provides a scoped IServiceProvider with an EventBus for proper
        // exception isolation semantics.
        Func<IServiceProvider, CancellationToken, Task> dispatch = (sp, ct) =>
        {
            var bus = sp.GetRequiredService<IEventBus>();
            return bus.PublishAsync(@event, ct);
        };

        if (!_channel.Writer.TryWrite(dispatch))
        {
            LogEventDropped(logger, typeof(T).Name);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Background event '{EventName}' dropped — channel closed"
    )]
    private static partial void LogEventDropped(ILogger logger, string eventName);
}
