using SimpleModule.Core.Broadcasting;

namespace SimpleModule.Hosting.Broadcasting;

/// <summary>
/// Tracks which connections are subscribed to which presence channels and
/// collapses multiple connections from the same user into a single
/// <see cref="PresenceMember"/>. Membership lives in memory — appropriate
/// for the single-instance default; horizontal scale-out requires a backplane.
/// </summary>
public sealed class PresenceTracker
{
    // Single mutex guards the whole map. Join/leave is not a hot path, and a
    // global lock makes the read-modify-write decisions ("was this the user's
    // first/last connection?") trivially atomic; a per-channel ConcurrentDictionary
    // would have races between the membership probe and the mutation.
    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, PresenceMember>> _channels = new(
        StringComparer.Ordinal
    );

    /// <summary>
    /// Registers <paramref name="member"/> on <paramref name="channel"/> for
    /// the given connection. Returns <c>true</c> if this is the user's first
    /// connection in the channel (so callers know whether to fire a "joined"
    /// notification).
    /// </summary>
    public bool Add(string channel, string connectionId, PresenceMember member)
    {
        lock (_gate)
        {
            if (!_channels.TryGetValue(channel, out var bucket))
            {
                bucket = new Dictionary<string, PresenceMember>(StringComparer.Ordinal);
                _channels[channel] = bucket;
            }

            var alreadyMember = bucket.Values.Any(m => m.UserId == member.UserId);
            bucket[connectionId] = member;
            return !alreadyMember;
        }
    }

    /// <summary>
    /// Removes the connection's membership from <paramref name="channel"/>.
    /// Returns <c>true</c> with the departing member if the user had no other
    /// connections in this channel — that's when a "left" event should fire.
    /// </summary>
    public bool Remove(string channel, string connectionId, out PresenceMember? member)
    {
        member = null;
        lock (_gate)
        {
            if (!_channels.TryGetValue(channel, out var bucket))
            {
                return false;
            }

            if (!bucket.Remove(connectionId, out var removed))
            {
                return false;
            }

            member = removed;
            return !bucket.Values.Any(m => m.UserId == removed.UserId);
        }
    }

    /// <summary>
    /// Drops every membership held by <paramref name="connectionId"/> across
    /// all channels (used on connection close). Returns the list of channels
    /// the user actually left (i.e. last connection in that channel) so the
    /// hub can announce departures.
    /// </summary>
    public IReadOnlyList<(string Channel, PresenceMember Member)> RemoveConnection(
        string connectionId
    )
    {
        lock (_gate)
        {
            var departures = new List<(string, PresenceMember)>();
            foreach (var (channel, bucket) in _channels)
            {
                if (
                    bucket.Remove(connectionId, out var removed)
                    && !bucket.Values.Any(m => m.UserId == removed.UserId)
                )
                {
                    departures.Add((channel, removed));
                }
            }
            return departures;
        }
    }

    /// <summary>Current member list (one entry per user, not per connection).</summary>
    public IReadOnlyList<PresenceMember> Members(string channel)
    {
        lock (_gate)
        {
            if (!_channels.TryGetValue(channel, out var bucket))
            {
                return Array.Empty<PresenceMember>();
            }

            return bucket
                .Values.GroupBy(m => m.UserId, StringComparer.Ordinal)
                .Select(g => g.First())
                .ToList();
        }
    }
}
