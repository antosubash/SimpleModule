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

public partial class LoginWithRecoveryCodeEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.LoginWithRecoveryCode;

    [LoggerMessage(Level = LogLevel.Information, Message = "User logged in with a recovery code.")]
    private static partial void LogLoggedInWithRecoveryCode(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User account locked out.")]
    private static partial void LogUserLockedOut(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid recovery code entered.")]
    private static partial void LogInvalidRecoveryCode(ILogger logger);

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
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
                Route,
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
                        LogLoggedInWithRecoveryCode(logger);
                        return TypedResults.Redirect(returnUrl ?? "/");
                    }

                    if (result.IsLockedOut)
                    {
                        LogUserLockedOut(logger);
                        return TypedResults.Redirect("/Identity/Account/Lockout");
                    }

                    LogInvalidRecoveryCode(logger);
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
