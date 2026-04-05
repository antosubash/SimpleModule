using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class LogoutEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Logout",
                (HttpContext context) =>
                {
                    var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
                    return Inertia.Render("Users/Account/Logout", new { isAuthenticated });
                }
            )
            .AllowAnonymous();

        app.MapPost(
                "/Logout",
                async (
                    [FromQuery] string? returnUrl,
                    SignInManager<ApplicationUser> signInManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    await signInManager.SignOutAsync();
                    logger.LogInformation("User logged out.");
                    return TypedResults.Redirect(returnUrl ?? "/");
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
