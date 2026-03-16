using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Endpoints.Account;

public static class AccountSecurityEndpoint
{
    private static readonly CompositeFormat AuthenticatorUriFormat = CompositeFormat.Parse(
        "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6"
    );

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/Identity/Account/Manage")
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();

        // GET /TwoFactorAuthentication
        group.MapGet(
            "/TwoFactorAuthentication",
            async (
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager
            ) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

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
        );

        // POST /TwoFactorAuthentication/forget-browser
        group.MapPost(
            "/TwoFactorAuthentication/forget-browser",
            async (
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager
            ) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                await signInManager.ForgetTwoFactorClientAsync();

                return Results.Redirect(
                    "/Identity/Account/Manage/TwoFactorAuthentication?status=browser-forgotten"
                );
            }
        );

        // GET /EnableAuthenticator
        group.MapGet(
            "/EnableAuthenticator",
            async (HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                var (sharedKey, authenticatorUri) = await LoadSharedKeyAndQrCodeUriAsync(
                    userManager,
                    user
                );

                return Inertia.Render(
                    "Users/Account/EnableAuthenticator",
                    new { sharedKey, authenticatorUri }
                );
            }
        );

        // POST /EnableAuthenticator
        group.MapPost(
            "/EnableAuthenticator",
            async (
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                ILogger<UsersModule> logger
            ) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                var form = await context.Request.ReadFormAsync();
                var code = form["code"].ToString();

                var verificationCode = code.Replace(" ", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal);

                var is2faTokenValid = await userManager.VerifyTwoFactorTokenAsync(
                    user,
                    userManager.Options.Tokens.AuthenticatorTokenProvider,
                    verificationCode
                );

                if (!is2faTokenValid)
                {
                    return Results.Redirect(
                        "/Identity/Account/Manage/EnableAuthenticator?error=invalid-code"
                    );
                }

                await userManager.SetTwoFactorEnabledAsync(user, true);
                logger.LogInformation("User has enabled 2FA with an authenticator app.");

                if (await userManager.CountRecoveryCodesAsync(user) == 0)
                {
                    var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                        user,
                        10
                    );

                    return Inertia.Render(
                        "Users/Account/ShowRecoveryCodes",
                        new
                        {
                            recoveryCodes = recoveryCodes!.ToArray(),
                            statusMessage = "Your authenticator app has been verified.",
                        }
                    );
                }

                return Results.Redirect(
                    "/Identity/Account/Manage/TwoFactorAuthentication?status=authenticator-verified"
                );
            }
        );

        // GET /Disable2fa
        group.MapGet(
            "/Disable2fa",
            async (HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                if (!await userManager.GetTwoFactorEnabledAsync(user))
                    return Results.Redirect("/Identity/Account/Manage/TwoFactorAuthentication");

                return Inertia.Render("Users/Account/Disable2fa", new { });
            }
        );

        // POST /Disable2fa
        group.MapPost(
            "/Disable2fa",
            async (
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                ILogger<UsersModule> logger
            ) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                await userManager.SetTwoFactorEnabledAsync(user, false);
                logger.LogInformation("User has disabled 2FA.");

                return Results.Redirect(
                    "/Identity/Account/Manage/TwoFactorAuthentication?status=2fa-disabled"
                );
            }
        );

        // GET /ResetAuthenticator
        group.MapGet(
            "/ResetAuthenticator",
            async (HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                return Inertia.Render("Users/Account/ResetAuthenticator", new { });
            }
        );

        // POST /ResetAuthenticator
        group.MapPost(
            "/ResetAuthenticator",
            async (
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                ILogger<UsersModule> logger
            ) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                await userManager.SetTwoFactorEnabledAsync(user, false);
                await userManager.ResetAuthenticatorKeyAsync(user);
                logger.LogInformation("User has reset their authentication app key.");

                await signInManager.RefreshSignInAsync(user);

                return Results.Redirect(
                    "/Identity/Account/Manage/EnableAuthenticator?status=authenticator-reset"
                );
            }
        );

        // GET /GenerateRecoveryCodes
        group.MapGet(
            "/GenerateRecoveryCodes",
            async (HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                if (!await userManager.GetTwoFactorEnabledAsync(user))
                    return Results.Redirect("/Identity/Account/Manage/TwoFactorAuthentication");

                return Inertia.Render("Users/Account/GenerateRecoveryCodes", new { });
            }
        );

        // POST /GenerateRecoveryCodes
        group.MapPost(
            "/GenerateRecoveryCodes",
            async (
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                ILogger<UsersModule> logger
            ) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                    return Results.Redirect("/Identity/Account/Login");

                if (!await userManager.GetTwoFactorEnabledAsync(user))
                    return Results.Redirect("/Identity/Account/Manage/TwoFactorAuthentication");

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

    private static async Task<(
        string sharedKey,
        string authenticatorUri
    )> LoadSharedKeyAndQrCodeUriAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user
    )
    {
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var sharedKey = FormatKey(unformattedKey!);
        var email = await userManager.GetEmailAsync(user);
        var authenticatorUri = string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            UrlEncoder.Default.Encode("SimpleModule"),
            UrlEncoder.Default.Encode(email!),
            unformattedKey
        );

        return (sharedKey, authenticatorUri);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }
#pragma warning disable CA1308 // Authenticator keys are conventionally displayed lowercase
        return result.ToString().ToLowerInvariant();
#pragma warning restore CA1308
    }
}
