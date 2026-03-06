using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using SimpleModule.Users.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.Users.Features.Connect;

public static class UserinfoEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapMethods(
                "/connect/userinfo",
                [HttpMethods.Get, HttpMethods.Post],
                (Delegate)HandleAsync
            )
            .RequireAuthorization()
            .ExcludeFromDescription();
    }

    private static async Task<IResult> HandleAsync(HttpContext context)
    {
        var userManager = context.RequestServices.GetRequiredService<
            UserManager<ApplicationUser>
        >();
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            return Results.Challenge(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]
            );
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = await userManager.GetUserIdAsync(user),
        };

        if (context.User.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = await userManager.GetEmailAsync(user) ?? string.Empty;
            claims[Claims.EmailVerified] = await userManager.IsEmailConfirmedAsync(user);
        }

        if (context.User.HasScope(Scopes.Profile))
        {
            claims[Claims.Name] = user.DisplayName;
        }

        return Results.Ok(claims);
    }
}
