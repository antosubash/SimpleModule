# Broadcasting

Server-to-browser push via SignalR. Modules raise events marked with
`[BroadcastEvent]` (or call `IBroadcaster` directly); the framework
forwards them to subscribed clients. See [`docs/broadcasting.md`](../../../docs/broadcasting.md)
for the full developer guide.

- `IBroadcastEvent` / `BroadcastEventAttribute` — opt-in marker
- `IBroadcaster` — fan-out service (channel / user / tenant)
- `IBroadcastChannelAuthorizer` — per-prefix subscription guards
- `BroadcastChannels` — naming helpers (`private-users.{id}`, `presence-...`)
- `BroadcastContext` — `ClaimsPrincipal` + tenant id passed to authorizers
- `BroadcastEnvelope`, `PresenceChange`, `PresenceMember` — wire payloads

The SignalR hub, broadcaster implementation, authorizer chain, and
Wolverine bridge live in `SimpleModule.Hosting/Broadcasting/`.
