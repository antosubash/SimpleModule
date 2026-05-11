using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Broadcasting;

namespace SimpleModule.Hosting.Broadcasting;

/// <summary>
/// SignalR hub clients connect to in order to subscribe/unsubscribe from
/// channels and receive broadcast envelopes. Authentication is required
/// for any private or presence channel; public channel subscriptions go
/// through the authorizer chain too (default policy rejects public
/// channels — see <see cref="DefaultDenyPublicAuthorizer"/>).
/// </summary>
[Authorize]
public sealed partial class BroadcastHub(
    BroadcastAuthorizerChain authorizers,
    PresenceTracker presence,
    ILogger<BroadcastHub> logger
) : Hub
{
    public const string Endpoint = "/hub/broadcast";

    public override async Task OnConnectedAsync()
    {
        LogConnected(logger, Context.ConnectionId, Context.UserIdentifier);

        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupForUser(userId));
            var tenantId = Context.User?.FindFirst(WellKnownClaims.TenantId)?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupForTenant(tenantId));
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var channels = presence.RemoveConnection(Context.ConnectionId);
        foreach (var (channel, member) in channels)
        {
            var snapshot = presence.Members(channel);
            await Clients
                .Group(GroupForChannel(channel))
                .SendAsync(
                    BroadcastClientMethods.PresenceChanged,
                    new PresenceChange(channel, PresenceChangeKind.Left, member, snapshot)
                );
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe the current connection to <paramref name="channel"/>.
    /// Returns the presence roster (empty for non-presence channels) so the
    /// client can render an initial member list without a follow-up roundtrip.
    /// </summary>
    public async Task<SubscribeResult> Subscribe(string channel)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            return SubscribeResult.Failed("channel name required");
        }

        var ctx = new BroadcastContext(
            Context.User,
            Context.User?.FindFirst(WellKnownClaims.TenantId)?.Value
        );

        var ok = await authorizers.AuthorizeAsync(channel, ctx, Context.ConnectionAborted);
        if (!ok)
        {
            LogSubscriptionDenied(logger, channel, Context.UserIdentifier);
            return SubscribeResult.Failed("not authorized");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupForChannel(channel));

        if (!BroadcastChannels.IsPresence(channel))
        {
            return SubscribeResult.Succeeded(Array.Empty<PresenceMember>());
        }

        var userId =
            Context.UserIdentifier
            ?? throw new HubException("presence channels require an authenticated user");

        var info = new Dictionary<string, string>(StringComparer.Ordinal);
        var name = Context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(name))
        {
            info["name"] = name;
        }

        var member = new PresenceMember(userId, info);
        var changed = presence.Add(channel, Context.ConnectionId, member);
        var members = presence.Members(channel);

        if (changed)
        {
            await Clients
                .Group(GroupForChannel(channel))
                .SendAsync(
                    BroadcastClientMethods.PresenceChanged,
                    new PresenceChange(channel, PresenceChangeKind.Joined, member, members)
                );
        }

        return SubscribeResult.Succeeded(members);
    }

    /// <summary>Unsubscribe the current connection from <paramref name="channel"/>.</summary>
    public async Task Unsubscribe(string channel)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupForChannel(channel));

        if (!BroadcastChannels.IsPresence(channel))
        {
            return;
        }

        if (presence.Remove(channel, Context.ConnectionId, out var member))
        {
            var members = presence.Members(channel);
            await Clients
                .Group(GroupForChannel(channel))
                .SendAsync(
                    BroadcastClientMethods.PresenceChanged,
                    new PresenceChange(channel, PresenceChangeKind.Left, member!, members)
                );
        }
    }

    internal static string GroupForChannel(string channel) => $"ch:{channel}";

    internal static string GroupForUser(string userId) => $"u:{userId}";

    internal static string GroupForTenant(string tenantId) => $"t:{tenantId}";

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Broadcast hub connection {ConnectionId} ({UserId})"
    )]
    private static partial void LogConnected(ILogger logger, string connectionId, string? userId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Broadcast subscription denied: channel={Channel} user={UserId}"
    )]
    private static partial void LogSubscriptionDenied(
        ILogger logger,
        string channel,
        string? userId
    );
}

/// <summary>
/// SignalR method names the hub invokes on clients. Keeping these as
/// constants prevents typos between server and TypeScript SDK.
/// </summary>
public static class BroadcastClientMethods
{
    public const string EventReceived = "broadcast";
    public const string PresenceChanged = "presence";
}

/// <summary>Result returned from <see cref="BroadcastHub.Subscribe"/>.</summary>
public sealed record SubscribeResult(
    bool Authorized,
    string? Reason,
    IReadOnlyList<PresenceMember> Members
)
{
    public static SubscribeResult Succeeded(IReadOnlyList<PresenceMember> members) =>
        new(true, null, members);

    public static SubscribeResult Failed(string reason) =>
        new(false, reason, Array.Empty<PresenceMember>());
}
