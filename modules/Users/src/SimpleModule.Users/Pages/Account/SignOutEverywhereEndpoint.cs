using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Contracts.Events;
using Wolverine;

namespace SimpleModule.Users.Pages.Account;

public partial class SignOutEverywhereEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.SignOutEverywhere;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "User {UserId} invoked sign-out-everywhere; security stamp regenerated."
    )]
    private static partial void LogSignedOutEverywhere(ILogger logger, string userId);

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                Route,
                async (
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager,
                    IMessageBus bus,
                    ILogger<UsersModule> logger
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    // Canonical Identity primitive for "invalidate every cookie / token issued
                    // before now". SecurityStampValidator on the cookie middleware checks this
                    // on each request (at the configured interval), so other devices die naturally.
                    await userManager.UpdateSecurityStampAsync(user);
                    await signInManager.SignOutAsync();

                    LogSignedOutEverywhere(logger, user.Id);

                    // OpenIddict subscribes to this event and revokes any active tokens. Going
                    // through the bus avoids a Users → OpenIddict reference, which the module
                    // graph forbids.
                    await bus.PublishAsync(new UserSignedOutEverywhereEvent(UserId.From(user.Id)));

                    return TypedResults.Redirect(
                        "/Identity/Account/Login?signedOutEverywhere=true"
                    );
                }
            )
            .DisableAntiforgery();
    }
}
