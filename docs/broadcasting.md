# Broadcasting

Real-time push from server to browser, layered on SignalR.

Modules emit domain events through the same `IMessageBus` they already
use; events that implement `IBroadcastEvent` are forwarded by a
framework-supplied Wolverine handler to whoever is subscribed to the
channel the event names. Public-channel subscriptions go through an
authorizer chain, and presence channels track members across multiple
connections per user.

## Hub endpoint

`/hub/broadcast` — SignalR hub mapped by the framework. Authentication is
mandatory (the `FallbackPolicy` is `RequireAuthenticatedUser`). The hub
exposes two client-callable methods:

- `Subscribe(channel)` → `SubscribeResult { authorized, reason, members }`
- `Unsubscribe(channel)` → `void`

And two server-to-client invocations:

- `broadcast` — `BroadcastEnvelope { channel, event, payload }`
- `presence` — `PresenceChange { channel, kind, member, members }`

## Server: emitting events

Mark an event with `[BroadcastEvent("wire.name")]` and implement
`IBroadcastEvent`. The wire name is what the browser subscribes to via
`useEvent(channel, 'wire.name', ...)`.

```csharp
using SimpleModule.Core.Broadcasting;
using SimpleModule.Core.Events;

[BroadcastEvent("notifications.created")]
public sealed record NotificationCreated(Guid UserId, string Title)
    : DomainEvent, IBroadcastEvent
{
    public string Channel(IBroadcastContext ctx) =>
        BroadcastChannels.ForUser(UserId.ToString());
}
```

Publish the event through Wolverine the same way you publish anything else:

```csharp
await bus.PublishAsync(new NotificationCreated(userId, "Welcome!"));
```

The framework decorates Wolverine's `IMessageBus` with
`BroadcastingMessageBus` (Scrutor decorator, same pattern as the audit
log). Any message that is an `IBroadcastEvent` is forwarded to
`IBroadcaster` after the inner bus accepts it — no per-event handler, no
opt-in registration. Forwarding errors are logged but never propagated:
the primary business operation must not fail because SignalR fan-out
failed.

### Calling the broadcaster directly

When you want a fire-and-forget push that isn't a domain event, inject
`IBroadcaster`:

```csharp
public sealed class ChatService(IBroadcaster broadcaster)
{
    public Task TypingAsync(string roomId, string userId, CancellationToken ct) =>
        broadcaster.ToChannelAsync(
            $"presence-rooms.{roomId}",
            "chat.typing",
            new { userId },
            ct
        );
}
```

`IBroadcaster` also exposes `ToUserAsync` / `ToTenantAsync` for the
implicit per-user and per-tenant fan-out groups the hub maintains for
every authenticated connection.

## Channel naming

Channel names are arbitrary strings, but the framework reserves two prefixes
(borrowed from Pusher / Laravel Echo):

- `private-` — requires authentication; the authorizer chain decides
  whether the connected principal may subscribe.
- `presence-` — same as private, plus the server tracks members and
  pushes join/leave deltas.

Helpers in `BroadcastChannels`:

- `BroadcastChannels.ForUser(userId)` → `private-users.{userId}`
- `BroadcastChannels.ForTenant(tenantId)` → `private-tenants.{tenantId}`

## Authorizers

Implement `IBroadcastChannelAuthorizer` and register it:

```csharp
public sealed class OrdersChannelAuthorizer(IOrdersContracts orders)
    : IBroadcastChannelAuthorizer
{
    public string ChannelPrefix => "private-tenants.";

    public async Task<bool> AuthorizeAsync(
        string channel,
        IBroadcastContext ctx,
        CancellationToken ct
    )
    {
        // private-tenants.{tid}.orders.{orderId}
        var parts = channel.Substring(ChannelPrefix.Length).Split('.');
        var tenantId = parts[0];
        var orderId = parts[^1];
        return await orders.UserCanSeeOrderAsync(ctx.User!, tenantId, orderId, ct);
    }
}

// In ConfigureServices:
services.AddBroadcastAuthorizer<OrdersChannelAuthorizer>();
```

The chain matches by longest prefix, so your specific rule overrides the
default tenant guard the framework ships.

## Browser: @simplemodule/echo

```tsx
import { EchoProvider, useEvent, usePresence } from '@simplemodule/echo';

// Mount once near the root, inside Inertia's app component.
<EchoProvider>
  <App />
</EchoProvider>;

function NotificationBell({ userId }: { userId: string }) {
  const [count, setCount] = useState(0);
  useEvent<NotificationCreated>(
    `private-users.${userId}`,
    'notifications.created',
    () => setCount((n) => n + 1)
  );
  return <span>{count}</span>;
}

function RoomRoster({ roomId }: { roomId: string }) {
  const members = usePresence(`presence-rooms.${roomId}`);
  return (
    <ul>
      {members.map((m) => (
        <li key={m.userId}>{m.info?.name ?? m.userId}</li>
      ))}
    </ul>
  );
}
```

The Echo client multiplexes every subscription onto a single WebSocket and
re-subscribes automatically after reconnects. Subscriptions are
reference-counted: mounting `useEvent` ten times against the same channel
costs one network call.

## Scaling notes

The framework's `PresenceTracker` stores membership in memory, which is
fine for a single instance. For horizontal scale-out, run SignalR with a
Redis backplane (`AddStackExchangeRedis(...)`) and replace the
`PresenceTracker` with a Redis-backed implementation — the
`IBroadcaster` / `IBroadcastChannelAuthorizer` contracts stay the same.
