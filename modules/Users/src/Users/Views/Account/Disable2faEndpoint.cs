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
                async (HttpContext context, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(context.User);
                    if (user is null)
                        return Results.Redirect("/Identity/Account/Login");

                    if (!await userManager.GetTwoFactorEnabledAsync(user))
                        return Results.Redirect("/Identity/Account/Manage/TwoFactorAuthentication");

                    return Inertia.Render("Users/Account/Disable2fa", new { });
                }
            )
            .RequireAuthorization();
    }
}
