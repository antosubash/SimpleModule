using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.LoadTests.Infrastructure;

/// <summary>
/// OpenIddict event handler that processes ROPC (password grant) token requests.
/// </summary>
public static class PasswordGrantTokenHandler
{
    public static async ValueTask Handle(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        var request = context.Request;
        if (!request.IsPasswordGrantType())
            return;

        var httpContext = context.Transaction.GetHttpRequest()?.HttpContext
            ?? throw new InvalidOperationException("HttpContext not available.");
        var sp = httpContext.RequestServices;

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(request.Username!);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password!))
        {
            context.Reject(Errors.InvalidGrant, "The username or password is invalid.");
            return;
        }

        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        var userId = await userManager.GetUserIdAsync(user);
        identity.SetClaim(Claims.Subject, userId)
            .SetClaim(Claims.Email, await userManager.GetEmailAsync(user) ?? string.Empty)
            .SetClaim(Claims.Name, user.DisplayName);

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        var permContracts = sp.GetRequiredService<IPermissionContracts>();
        var userContracts = sp.GetRequiredService<IUserContracts>();
        var userIdTyped = UserId.From(userId);
        var roleIdMap = await userContracts.GetRoleIdsByNamesAsync(roles);
        var allPermissions = await permContracts.GetAllPermissionsForUserAsync(
            userIdTyped,
            roleIdMap.Values.Select(id => RoleId.From(id)));

        foreach (var permission in allPermissions)
        {
            identity.AddClaim("permission", permission);
        }

        identity.SetScopes(request.GetScopes());

        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, identity));
        }

        context.SignIn(new ClaimsPrincipal(identity));
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity identity)
    {
        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;
                if (identity.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                if (identity.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                if (identity.HasScope(AuthConstants.RolesScope))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            case "permission":
                yield return Destinations.AccessToken;
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
