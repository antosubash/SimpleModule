using SimpleModule.Core.Authorization;
using SimpleModule.Core.Broadcasting;
using SimpleModule.Core.Extensions;

namespace SimpleModule.Hosting.Broadcasting;

/// <summary>
/// Runs the registered <see cref="IBroadcastChannelAuthorizer"/> with the
/// longest matching <see cref="IBroadcastChannelAuthorizer.ChannelPrefix"/>
/// for the requested channel. Longest-prefix-wins lets modules ship narrow
/// rules (<c>private-tenants.</c>) without colliding with the framework's
/// catch-all deny on bare public channels.
/// </summary>
public sealed class BroadcastAuthorizerChain(IEnumerable<IBroadcastChannelAuthorizer> authorizers)
{
    private readonly IBroadcastChannelAuthorizer[] _ordered = authorizers
        .OrderByDescending(a => a.ChannelPrefix.Length)
        .ToArray();

    public async Task<bool> AuthorizeAsync(
        string channel,
        IBroadcastContext context,
        CancellationToken cancellationToken
    )
    {
        foreach (var authorizer in _ordered)
        {
            if (
                authorizer.ChannelPrefix.Length == 0
                || channel.StartsWith(authorizer.ChannelPrefix, StringComparison.Ordinal)
            )
            {
                return await authorizer.AuthorizeAsync(channel, context, cancellationToken);
            }
        }

        return false;
    }
}

/// <summary>
/// Default catch-all authorizer. Allows public channels (no prefix) and
/// allows private/presence channels only for authenticated principals — the
/// owning module is expected to ship a tighter rule (e.g., only the channel's
/// tenant or user) on top of this.
/// </summary>
public sealed class DefaultBroadcastAuthorizer : IBroadcastChannelAuthorizer
{
    public string ChannelPrefix => string.Empty;

    public Task<bool> AuthorizeAsync(
        string channel,
        IBroadcastContext context,
        CancellationToken cancellationToken
    )
    {
        if (BroadcastChannels.IsPrivate(channel))
        {
            var authenticated = context.User?.Identity?.IsAuthenticated == true;
            return Task.FromResult(authenticated);
        }

        // Public channels are off by default; user code opts in by registering
        // an authorizer for the specific prefix that should be open.
        return Task.FromResult(false);
    }
}

/// <summary>
/// Restricts <c>private-users.{userId}</c> channels to the matching principal.
/// Registered by default so modules don't have to re-implement the "only the
/// user themselves" check for personal channels.
/// </summary>
public sealed class UserChannelAuthorizer : IBroadcastChannelAuthorizer
{
    private const string Prefix = BroadcastChannels.PrivatePrefix + "users.";

    public string ChannelPrefix => Prefix;

    public Task<bool> AuthorizeAsync(
        string channel,
        IBroadcastContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(false);
        }

        var requested = channel.Substring(Prefix.Length);
        var current = context.User.GetUserId();

        return Task.FromResult(string.Equals(requested, current, StringComparison.Ordinal));
    }
}

/// <summary>
/// Restricts <c>private-tenants.{tenantId}</c> channels (and descendants) to
/// principals whose <c>tenantid</c> claim matches.
/// </summary>
public sealed class TenantChannelAuthorizer : IBroadcastChannelAuthorizer
{
    private const string Prefix = BroadcastChannels.PrivatePrefix + "tenants.";

    public string ChannelPrefix => Prefix;

    public Task<bool> AuthorizeAsync(
        string channel,
        IBroadcastContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(false);
        }

        var trail = channel.Substring(Prefix.Length);
        var requested = trail.Split('.', 2)[0];
        var current = context.TenantId ?? context.User.FindFirst(WellKnownClaims.TenantId)?.Value;

        return Task.FromResult(
            !string.IsNullOrEmpty(current)
                && string.Equals(requested, current, StringComparison.Ordinal)
        );
    }
}
