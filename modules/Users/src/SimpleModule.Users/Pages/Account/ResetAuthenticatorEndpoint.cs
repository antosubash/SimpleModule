using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class ResetAuthenticatorEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.ResetAuthenticator;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    return Inertia.Render("Users/Account/ResetAuthenticator", new { });
                }
            )
            .RequireAuthorization();
    }
}
