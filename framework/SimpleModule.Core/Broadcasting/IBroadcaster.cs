namespace SimpleModule.Core.Broadcasting;

/// <summary>
/// Push real-time payloads to connected browser clients. Implementations are
/// expected to fan out via SignalR (or any equivalent transport); event names
/// are arbitrary strings agreed between server and client.
/// </summary>
public interface IBroadcaster
{
    /// <summary>
    /// Send <paramref name="payload"/> to everyone subscribed to
    /// <paramref name="channel"/>. Channels do not have to be pre-declared —
    /// they are created on first subscription.
    /// </summary>
    Task ToChannelAsync(
        string channel,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Send <paramref name="payload"/> to every connection whose principal has
    /// the given <paramref name="userId"/>. A single user may have many
    /// connections (multiple browser tabs); all of them receive the event.
    /// </summary>
    Task ToUserAsync(
        string userId,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Send <paramref name="payload"/> to every connection whose principal
    /// belongs to <paramref name="tenantId"/>. Maps to the implicit
    /// <c>tenants.{tenantId}</c> channel managed by the framework.
    /// </summary>
    Task ToTenantAsync(
        string tenantId,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Forward an <see cref="IBroadcastEvent"/> to its declared channel,
    /// using the event name from <see cref="BroadcastEventAttribute"/> if
    /// present, otherwise the CLR type name.
    /// </summary>
    Task PublishAsync(
        IBroadcastEvent broadcastEvent,
        CancellationToken cancellationToken = default
    );
}
