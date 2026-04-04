using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Views.Account;

[ViewPage("Users/Account/LoginWith2fa")]
public class LoginWith2faEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/LoginWith2fa",
                async (
                    [FromQuery] bool rememberMe,
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
                        "Users/Account/LoginWith2fa",
                        new { rememberMe, returnUrl = returnUrl ?? "/" }
                    );
                }
            )
            .AllowAnonymous();

        app.MapPost(
                "/LoginWith2fa",
                async (
                    [FromForm] string twoFactorCode,
                    [FromForm] bool rememberMachine,
                    [FromQuery] bool rememberMe,
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

                    var authenticatorCode = twoFactorCode
                        .Replace(" ", string.Empty, StringComparison.Ordinal)
                        .Replace("-", string.Empty, StringComparison.Ordinal);

                    var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                        authenticatorCode,
                        rememberMe,
                        rememberMachine
                    );

                    if (result.Succeeded)
                    {
                        logger.LogInformation("User logged in with 2fa.");
                        return TypedResults.Redirect(returnUrl ?? "/");
                    }

                    if (result.IsLockedOut)
                    {
                        logger.LogWarning("User account locked out.");
                        return TypedResults.Redirect("/Identity/Account/Lockout");
                    }

                    logger.LogWarning("Invalid authenticator code entered.");
                    return Inertia.Render(
                        "Users/Account/LoginWith2fa",
                        new
                        {
                            rememberMe,
                            returnUrl = returnUrl ?? "/",
                            errors = (string[])["Invalid authenticator code."],
                        }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
