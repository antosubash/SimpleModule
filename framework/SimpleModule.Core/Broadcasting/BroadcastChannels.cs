namespace SimpleModule.Core.Broadcasting;

/// <summary>
/// Canonical channel-name helpers. Keeping channel naming in one place stops
/// modules and the client SDK from drifting on how a user- or tenant-scoped
/// channel is spelled.
/// </summary>
public static class BroadcastChannels
{
    /// <summary>Prefix marking a channel that requires authentication (no metadata beyond auth).</summary>
    public const string PrivatePrefix = "private-";

    /// <summary>Prefix marking a presence channel — server tracks members and pushes join/leave.</summary>
    public const string PresencePrefix = "presence-";

    public static string ForUser(string userId) => $"{PrivatePrefix}users.{userId}";

    public static string ForTenant(string tenantId) => $"{PrivatePrefix}tenants.{tenantId}";

    /// <summary>True if <paramref name="channel"/> requires the subscriber to be authenticated.</summary>
    public static bool IsPrivate(string channel) =>
        channel.StartsWith(PrivatePrefix, StringComparison.Ordinal)
        || channel.StartsWith(PresencePrefix, StringComparison.Ordinal);

    public static bool IsPresence(string channel) =>
        channel.StartsWith(PresencePrefix, StringComparison.Ordinal);
}
