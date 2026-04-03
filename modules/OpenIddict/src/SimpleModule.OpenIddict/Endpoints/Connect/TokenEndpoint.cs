using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using SimpleModule.Core;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.OpenIddict.Endpoints.Connect;

public class TokenEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(ConnectRouteConstants.ConnectToken, (Delegate)HandleAsync)
            .ExcludeFromDescription()
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(HttpContext context)
    {
        var request =
            context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(AuthErrorMessages.OpenIdConnectRequestMissing);

        if (!request.IsPasswordGrantType())
        {
            // Let OpenIddict handle non-password grants (auth code, refresh token)
            return Results.Empty;
        }

        var userManager = context.RequestServices.GetRequiredService<
            UserManager<ApplicationUser>
        >();

        var user = await userManager.FindByEmailAsync(request.Username!);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password!))
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]
            );
        }

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var userIdString = await userManager.GetUserIdAsync(user);
        identity
            .SetClaim(Claims.Subject, userIdString)
            .SetClaim(Claims.Email, await userManager.GetEmailAsync(user) ?? string.Empty)
            .SetClaim(Claims.Name, user.DisplayName);

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        // Load permissions
        var permissionContracts =
            context.RequestServices.GetRequiredService<IPermissionContracts>();
        var userContracts = context.RequestServices.GetRequiredService<IUserContracts>();
        var userId = UserId.From(userIdString);

        var roleIdMap = await userContracts.GetRoleIdsByNamesAsync(roles);

        var allPermissions = await permissionContracts.GetAllPermissionsForUserAsync(
            userId,
            roleIdMap.Values.Select(id => RoleId.From(id))
        );

        foreach (var permission in allPermissions)
        {
            identity.AddClaim("permission", permission);
        }

        identity.SetScopes(
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Email,
            AuthConstants.RolesScope
        );

        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, identity));
        }

        var principal = new ClaimsPrincipal(identity);

        return Results.SignIn(
            principal,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
        );
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
