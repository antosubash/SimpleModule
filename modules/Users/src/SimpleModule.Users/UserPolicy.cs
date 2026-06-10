using System.Security.Claims;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Extensions;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users;

/// <summary>
/// Instance-level rules for user accounts, layered on the <c>Users.View/Update/Delete</c>
/// permission gates: a non-admin may view or update only their own account, and only
/// admins may delete accounts. View denials use <c>DenyAsNotFound</c> so a non-admin
/// cannot probe which account IDs exist. Auto-registered by the source generator.
/// </summary>
public sealed class UserPolicy : IPolicy<UserDto>
{
    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        string action,
        UserDto resource,
        CancellationToken cancellationToken = default
    )
    {
        var isAdmin = user.IsInRole(WellKnownRoles.Admin);
        var result = action switch
        {
            PolicyActions.View => isAdmin || IsSelf(user, resource)
                ? AuthorizationResult.Allow()
                : AuthorizationResult.DenyAsNotFound("You can only view your own account."),
            PolicyActions.Update => isAdmin || IsSelf(user, resource)
                ? AuthorizationResult.Allow()
                : AuthorizationResult.Deny("You can only update your own account."),
            PolicyActions.Delete => isAdmin
                ? AuthorizationResult.Allow()
                : AuthorizationResult.Deny("Only administrators can delete accounts."),
            _ => AuthorizationResult.Deny($"Unknown user action '{action}'."),
        };
        return Task.FromResult(result);
    }

    private static bool IsSelf(ClaimsPrincipal user, UserDto resource)
    {
        var userId = user.GetUserId();
        return !string.IsNullOrEmpty(userId) && resource.Id == UserId.From(userId);
    }
}
