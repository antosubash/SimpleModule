namespace SimpleModule.Core.Broadcasting;

/// <summary>
/// Wire format the hub sends to clients. The client SDK dispatches by
/// <see cref="Event"/> so a single channel can carry many event types.
/// </summary>
/// <param name="Channel">Channel the payload was published to.</param>
/// <param name="Event">Event name (matches <see cref="BroadcastEventAttribute.Name"/> when forwarded from <see cref="IBroadcastEvent"/>).</param>
/// <param name="Payload">JSON-serializable payload — usually the event record itself.</param>
public sealed record BroadcastEnvelope(string Channel, string Event, object Payload);

/// <summary>
/// Presence membership delta pushed to subscribers of a presence channel
/// whenever members join or leave. The framework synthesizes these from
/// connection lifecycle events; user code does not raise them directly.
/// </summary>
public sealed record PresenceChange(
    string Channel,
    PresenceChangeKind Kind,
    PresenceMember Member,
    IReadOnlyList<PresenceMember> Members
);

public enum PresenceChangeKind
{
    Joined,
    Left,
}

/// <summary>
/// Snapshot of a presence-channel member. <see cref="UserId"/> is the stable
/// identity (multiple connections from the same user collapse to one
/// member); <see cref="Info"/> carries any extra metadata the authorizer
/// chose to attach (display name, avatar, role).
/// </summary>
public sealed record PresenceMember(
    string UserId,
    IReadOnlyDictionary<string, string>? Info = null
);
