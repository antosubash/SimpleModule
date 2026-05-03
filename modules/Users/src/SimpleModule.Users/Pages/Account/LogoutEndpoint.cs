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

public partial class LogoutEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.Logout;

    [LoggerMessage(Level = LogLevel.Information, Message = "User logged out.")]
    private static partial void LogUserLoggedOut(ILogger logger);

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                (HttpContext context) =>
                {
                    var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
                    return Inertia.Render("Users/Account/Logout", new { isAuthenticated });
                }
            )
            .AllowAnonymous();

        app.MapPost(
                Route,
                async (
                    [FromQuery] string? returnUrl,
                    SignInManager<ApplicationUser> signInManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    await signInManager.SignOutAsync();
                    LogUserLoggedOut(logger);
                    return TypedResults.Redirect(returnUrl ?? "/");
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
