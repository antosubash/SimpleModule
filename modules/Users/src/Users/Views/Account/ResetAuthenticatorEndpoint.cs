using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Views.Account;

public class ResetAuthenticatorEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Identity/Account/Manage/ResetAuthenticator",
                async (HttpContext context, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(context.User);
                    if (user is null)
                        return Results.Redirect("/Identity/Account/Login");

                    return Inertia.Render("Users/Account/ResetAuthenticator", new { });
                }
            )
            .RequireAuthorization();
    }
}
