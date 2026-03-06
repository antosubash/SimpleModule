using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core.Events;

public sealed class EventBus(IServiceProvider serviceProvider) : IEventBus
{
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<T>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}
