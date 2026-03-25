using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Views.Account;

public class Disable2faEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Identity/Account/Manage/Disable2fa",
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    if (!await userManager.GetTwoFactorEnabledAsync(user))
                        return TypedResults.Redirect("/Identity/Account/Manage/TwoFactorAuthentication");

                    return Inertia.Render("Users/Account/Disable2fa", new { });
                }
            )
            .RequireAuthorization();
    }
}
