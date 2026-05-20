using SimpleModule.Core.Events;

namespace SimpleModule.Core.Broadcasting;

/// <summary>
/// Marker contract for domain events that should be forwarded to connected
/// browsers via SignalR. An <see cref="IBroadcastEvent"/> participates in the
/// same Wolverine pipeline as any other <see cref="IEvent"/>, but a generated
/// bridge handler also relays it through <see cref="IBroadcaster"/> to whoever
/// is subscribed to the channel returned by <see cref="Channel"/>.
/// </summary>
public interface IBroadcastEvent : IEvent
{
    /// <summary>
    /// Channel name this event is broadcast on. Channels are stable, hierarchical
    /// strings (e.g., <c>tenants.123.orders</c>). Authorizers run against the
    /// channel name before a client is allowed to subscribe.
    /// </summary>
    string Channel(IBroadcastContext context);
}
