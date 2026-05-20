using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SimpleModule.Core.Broadcasting;

namespace SimpleModule.Hosting.Broadcasting;

/// <summary>
/// SignalR-backed <see cref="IBroadcaster"/>. Channels map 1:1 to SignalR
/// groups (<c>ch:{name}</c>); per-user and per-tenant fan-out reuse the same
/// group convention so modules don't have to manage subscriptions for their
/// own private channels.
/// </summary>
public sealed class Broadcaster(IHubContext<BroadcastHub> hub) : IBroadcaster
{
    private static readonly ConcurrentDictionary<Type, string> _eventNameCache = new();

    public Task ToChannelAsync(
        string channel,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    ) =>
        hub
            .Clients.Group(BroadcastHub.GroupForChannel(channel))
            .SendAsync(
                BroadcastClientMethods.EventReceived,
                new BroadcastEnvelope(channel, @event, payload),
                cancellationToken
            );

    public Task ToUserAsync(
        string userId,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    ) =>
        hub
            .Clients.Group(BroadcastHub.GroupForUser(userId))
            .SendAsync(
                BroadcastClientMethods.EventReceived,
                new BroadcastEnvelope(BroadcastChannels.ForUser(userId), @event, payload),
                cancellationToken
            );

    public Task ToTenantAsync(
        string tenantId,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    ) =>
        hub
            .Clients.Group(BroadcastHub.GroupForTenant(tenantId))
            .SendAsync(
                BroadcastClientMethods.EventReceived,
                new BroadcastEnvelope(BroadcastChannels.ForTenant(tenantId), @event, payload),
                cancellationToken
            );

    public Task PublishAsync(
        IBroadcastEvent broadcastEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(broadcastEvent);
        var channel = broadcastEvent.Channel(BroadcastContext.Empty);
        var name = EventNameFor(broadcastEvent.GetType());
        return ToChannelAsync(channel, name, broadcastEvent, cancellationToken);
    }

    internal static string EventNameFor(Type eventType) =>
        _eventNameCache.GetOrAdd(
            eventType,
            static t =>
            {
                var attr = (BroadcastEventAttribute?)
                    Attribute.GetCustomAttribute(t, typeof(BroadcastEventAttribute));
                return attr?.Name ?? t.Name;
            }
        );
}
