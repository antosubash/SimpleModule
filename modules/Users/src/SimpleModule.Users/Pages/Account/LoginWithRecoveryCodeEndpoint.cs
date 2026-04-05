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

public class LoginWithRecoveryCodeEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/LoginWithRecoveryCode",
                async (
                    [FromQuery] string? returnUrl,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    return Inertia.Render(
                        "Users/Account/LoginWithRecoveryCode",
                        new { returnUrl = returnUrl ?? "/" }
                    );
                }
            )
            .AllowAnonymous();

        app.MapPost(
                "/LoginWithRecoveryCode",
                async (
                    [FromForm] string recoveryCode,
                    [FromQuery] string? returnUrl,
                    SignInManager<ApplicationUser> signInManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    var code = recoveryCode.Replace(" ", string.Empty, StringComparison.Ordinal);
                    var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(code);

                    if (result.Succeeded)
                    {
                        logger.LogInformation("User logged in with a recovery code.");
                        return TypedResults.Redirect(returnUrl ?? "/");
                    }

                    if (result.IsLockedOut)
                    {
                        logger.LogWarning("User account locked out.");
                        return TypedResults.Redirect("/Identity/Account/Lockout");
                    }

                    logger.LogWarning("Invalid recovery code entered.");
                    return Inertia.Render(
                        "Users/Account/LoginWithRecoveryCode",
                        new
                        {
                            returnUrl = returnUrl ?? "/",
                            errors = (string[])["Invalid recovery code entered."],
                        }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
