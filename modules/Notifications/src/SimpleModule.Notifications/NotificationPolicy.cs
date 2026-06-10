using System.Security.Claims;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Extensions;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications;

/// <summary>
/// Instance-level rules for notifications: only the recipient (or an admin) may view or
/// mark a notification read. The permission gate (<c>Notifications.ViewOwn</c>) stays on
/// the endpoint; this policy adds the per-resource ownership check. Auto-registered by
/// the source generator.
/// </summary>
public sealed class NotificationPolicy : IPolicy<Notification>
{
    /// <summary>Module-specific action beyond the conventional CRUD verbs.</summary>
    public const string MarkRead = "markRead";

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        string action,
        Notification resource,
        CancellationToken cancellationToken = default
    )
    {
        var result = action switch
        {
            PolicyActions.View or MarkRead => AllowOwnerOrAdmin(user, resource),
            _ => AuthorizationResult.Deny($"Unknown notification action '{action}'."),
        };
        return Task.FromResult(result);
    }

    private static AuthorizationResult AllowOwnerOrAdmin(
        ClaimsPrincipal user,
        Notification notification
    )
    {
        if (user.IsInRole(WellKnownRoles.Admin))
        {
            return AuthorizationResult.Allow();
        }

        var userId = user.GetUserId();
        return userId is not null && notification.UserId == UserId.From(userId)
            ? AuthorizationResult.Allow()
            : AuthorizationResult.Deny("You can only access your own notifications.");
    }
}
