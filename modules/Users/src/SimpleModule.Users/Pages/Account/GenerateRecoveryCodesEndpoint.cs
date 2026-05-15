using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

// Recovery codes are stored hashed (like passwords). There is intentionally
// no "get my existing codes" endpoint — once the user closes the
// ShowRecoveryCodes page they can only download/print the fresh set if they
// did so at generation time, or regenerate, which invalidates the old set.
// Do not add a "retrieve codes" code path; the cryptographic contract makes
// it impossible to honor and users would build a false expectation.
public class GenerateRecoveryCodesEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.GenerateRecoveryCodes;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
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
