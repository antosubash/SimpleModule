namespace SimpleModule.Core.Broadcasting;

/// <summary>
/// Decides whether the current principal is allowed to subscribe to a given
/// channel. Authorizers are matched by channel name prefix
/// (<see cref="ChannelPrefix"/>); the longest matching prefix wins so
/// modules can declare specific guards (<c>tenants.{tid}.orders</c>) while
/// the framework owns broader ones (<c>tenants.</c>).
/// </summary>
public interface IBroadcastChannelAuthorizer
{
    /// <summary>
    /// Channel name prefix this authorizer claims. Use a literal channel name
    /// for an exact match, or a trailing-dot prefix (e.g., <c>tenants.</c>)
    /// to match all descendants. <c>""</c> matches everything (used by the
    /// default deny-public-channels authorizer).
    /// </summary>
    string ChannelPrefix { get; }

    /// <summary>
    /// Returns <c>true</c> if the principal in <paramref name="context"/> may
    /// subscribe to <paramref name="channel"/>. The authorizer is also
    /// expected to filter channels that don't belong to the current tenant
    /// even when the prefix happens to match.
    /// </summary>
    Task<bool> AuthorizeAsync(
        string channel,
        IBroadcastContext context,
        CancellationToken cancellationToken = default
    );
}
