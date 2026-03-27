using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Server.AspNetCore;
using SimpleModule.Core;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.OpenIddict.Endpoints.Connect;

public class LogoutEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapMethods(
                ConnectRouteConstants.ConnectEndSession,
                [HttpMethods.Get, HttpMethods.Post],
                (Delegate)HandleAsync
            )
            .ExcludeFromDescription()
            .AllowAnonymous();
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
