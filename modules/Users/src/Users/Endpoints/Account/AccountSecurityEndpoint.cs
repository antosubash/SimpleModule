using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Endpoints.Account;

public class AccountSecurityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Identity/Account/Manage")
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();

        // POST /TwoFactorAuthentication/forget-browser
        group.MapPost(
            "/TwoFactorAuthentication/forget-browser",
            async (
                ClaimsPrincipal principal,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager
            ) =>
            {
                var user = await userManager.GetUserAsync(principal);
                if (user is null)
                    return TypedResults.Redirect("/Identity/Account/Login");

                await signInManager.ForgetTwoFactorClientAsync();

                return TypedResults.Redirect(
                    "/Identity/Account/Manage/TwoFactorAuthentication?status=browser-forgotten"
                );
            }
        );

        // POST /EnableAuthenticator
        group
            .MapPost(
                "/EnableAuthenticator",
                async (
                    [FromForm] string code,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    var verificationCode = code.Replace(" ", string.Empty, StringComparison.Ordinal)
                        .Replace("-", string.Empty, StringComparison.Ordinal);

                    var is2faTokenValid = await userManager.VerifyTwoFactorTokenAsync(
                        user,
                        userManager.Options.Tokens.AuthenticatorTokenProvider,
                        verificationCode
                    );

                    if (!is2faTokenValid)
                    {
                        return TypedResults.Redirect(
                            "/Identity/Account/Manage/EnableAuthenticator?error=invalid-code"
                        );
                    }

                    await userManager.SetTwoFactorEnabledAsync(user, true);
                    logger.LogInformation("User has enabled 2FA with an authenticator app.");

                    if (await userManager.CountRecoveryCodesAsync(user) == 0)
                    {
                        var recoveryCodes =
                            await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

                        return Inertia.Render(
                            "Users/Account/ShowRecoveryCodes",
                            new
                            {
                                recoveryCodes = recoveryCodes!.ToArray(),
                                statusMessage = "Your authenticator app has been verified.",
                            }
                        );
                    }

                    return TypedResults.Redirect(
                        "/Identity/Account/Manage/TwoFactorAuthentication?status=authenticator-verified"
                    );
                }
            )
            .DisableAntiforgery();

        // POST /Disable2fa
        group.MapPost(
            "/Disable2fa",
            async (
                ClaimsPrincipal principal,
                UserManager<ApplicationUser> userManager,
                ILogger<UsersModule> logger
            ) =>
            {
                var user = await userManager.GetUserAsync(principal);
                if (user is null)
                    return TypedResults.Redirect("/Identity/Account/Login");

                await userManager.SetTwoFactorEnabledAsync(user, false);
                logger.LogInformation("User has disabled 2FA.");

                return TypedResults.Redirect(
                    "/Identity/Account/Manage/TwoFactorAuthentication?status=2fa-disabled"
                );
            }
        );

        // POST /ResetAuthenticator
        group.MapPost(
            "/ResetAuthenticator",
            async (
                ClaimsPrincipal principal,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                ILogger<UsersModule> logger
            ) =>
            {
                var user = await userManager.GetUserAsync(principal);
                if (user is null)
                    return TypedResults.Redirect("/Identity/Account/Login");

                await userManager.SetTwoFactorEnabledAsync(user, false);
                await userManager.ResetAuthenticatorKeyAsync(user);
                logger.LogInformation("User has reset their authentication app key.");

                await signInManager.RefreshSignInAsync(user);

                return TypedResults.Redirect(
                    "/Identity/Account/Manage/EnableAuthenticator?status=authenticator-reset"
                );
            }
        );

        // POST /GenerateRecoveryCodes
        group.MapPost(
            "/GenerateRecoveryCodes",
            async (
                ClaimsPrincipal principal,
                UserManager<ApplicationUser> userManager,
                ILogger<UsersModule> logger
            ) =>
            {
                var user = await userManager.GetUserAsync(principal);
                if (user is null)
                    return TypedResults.Redirect("/Identity/Account/Login");

                if (!await userManager.GetTwoFactorEnabledAsync(user))
                    return TypedResults.Redirect("/Identity/Account/Manage/TwoFactorAuthentication");

                var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                    user,
                    10
                );
                logger.LogInformation("User has generated new 2FA recovery codes.");

                return Inertia.Render(
                    "Users/Account/ShowRecoveryCodes",
                    new
                    {
                        recoveryCodes = recoveryCodes!.ToArray(),
                        statusMessage = "You have generated new recovery codes.",
                    }
                );
            }
        );
    }
}
