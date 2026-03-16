using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using SimpleModule.Core;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.Users.Endpoints.Connect;

public class AuthorizationEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapMethods(
                ConnectRouteConstants.ConnectAuthorize,
                [HttpMethods.Get, HttpMethods.Post],
                (Delegate)HandleAsync
            )
            .ExcludeFromDescription();
    }

    private static async Task<IResult> HandleAsync(HttpContext context)
    {
        var request =
            context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(AuthErrorMessages.OpenIdConnectRequestMissing);

        var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!result.Succeeded)
        {
            return Results.Challenge(
                properties: new AuthenticationProperties
                {
                    RedirectUri =
                        context.Request.PathBase
                        + context.Request.Path
                        + QueryString.Create(
                            context.Request.HasFormContentType
                                ? context.Request.Form.Select(kvp => new KeyValuePair<
                                    string,
                                    string?
                                >(kvp.Key, kvp.Value))
                                : context.Request.Query.Select(kvp => new KeyValuePair<
                                    string,
                                    string?
                                >(kvp.Key, kvp.Value))
                        ),
                },
                authenticationSchemes: [IdentityConstants.ApplicationScheme]
            );
        }

        var userManager = context.RequestServices.GetRequiredService<
            UserManager<ApplicationUser>
        >();
        var user =
            await userManager.GetUserAsync(result.Principal)
            ?? throw new InvalidOperationException(AuthErrorMessages.UserDetailsMissing);

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        identity
            .SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
            .SetClaim(Claims.Email, await userManager.GetEmailAsync(user) ?? string.Empty)
            .SetClaim(Claims.Name, user.DisplayName);

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        identity.SetScopes(request.GetScopes());

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

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
