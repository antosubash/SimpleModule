using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Server.AspNetCore;
using SimpleModule.Core.Constants;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Features.Connect;

public static class LogoutEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapMethods(
                RouteConstants.ConnectEndSession,
                [HttpMethods.Get, HttpMethods.Post],
                (Delegate)HandleAsync
            )
            .ExcludeFromDescription();
    }

    private static async Task<IResult> HandleAsync(HttpContext context)
    {
        var signInManager = context.RequestServices.GetRequiredService<
            SignInManager<ApplicationUser>
        >();
        await signInManager.SignOutAsync();

        return Results.SignOut(
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties(),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]
        );
    }
}
