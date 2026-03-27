using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Views.Account;

public class TwoFactorAuthenticationEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Identity/Account/Manage/TwoFactorAuthentication",
                async (
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    return Inertia.Render(
                        "Users/Account/TwoFactorAuthentication",
                        new
                        {
                            hasAuthenticator = await userManager.GetAuthenticatorKeyAsync(user)
                                is not null,
                            is2faEnabled = await userManager.GetTwoFactorEnabledAsync(user),
                            isMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(
                                user
                            ),
                            recoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user),
                        }
                    );
                }
            )
            .RequireAuthorization();
    }
}
