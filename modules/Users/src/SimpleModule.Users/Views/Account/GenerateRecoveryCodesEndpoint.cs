using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Views.Account;

[ViewPage("Users/Account/GenerateRecoveryCodes")]
public class GenerateRecoveryCodesEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/GenerateRecoveryCodes",
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    if (!await userManager.GetTwoFactorEnabledAsync(user))
                        return TypedResults.Redirect(
                            "/Identity/Account/Manage/TwoFactorAuthentication"
                        );

                    return Inertia.Render("Users/Account/GenerateRecoveryCodes", new { });
                }
            )
            .RequireAuthorization();
    }
}
