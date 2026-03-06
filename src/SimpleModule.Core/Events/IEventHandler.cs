namespace SimpleModule.Core.Events;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - EventHandler is intentional
public interface IEventHandler<in T>
    where T : IEvent
{
    Task HandleAsync(T @event, CancellationToken cancellationToken);
}
#pragma warning restore CA1711
