using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class LoginEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Login",
                async (
                    HttpContext context,
                    ISettingsContracts settingsService,
                    ISettingsDefinitionRegistry settingsDefinitions,
                    [FromQuery] string? returnUrl
                ) =>
                {
                    await context.SignOutAsync(IdentityConstants.ExternalScheme);

                    var showTestAccounts = await settingsService.GetSettingAsync(
                        ConfigKeys.ShowTestAccounts,
                        SettingScope.System
                    );
                    showTestAccounts ??= settingsDefinitions
                        .GetDefinition(ConfigKeys.ShowTestAccounts)
                        ?.DefaultValue;

                    return Inertia.Render(
                        "Users/Account/Login",
                        new
                        {
                            returnUrl = returnUrl ?? "/",
                            showTestAccounts = showTestAccounts == "true",
                        }
                    );
                }
            )
            .AllowAnonymous();

        app.MapPost(
                "/Login",
                async (
                    [FromForm] string email,
                    [FromForm] string password,
                    [FromForm] bool? rememberMe,
                    [FromQuery] string? returnUrl,
                    SignInManager<ApplicationUser> signInManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    var result = await signInManager.PasswordSignInAsync(
                        email,
                        password,
                        rememberMe ?? false,
                        lockoutOnFailure: false
                    );

                    if (result.Succeeded)
                    {
                        logger.LogInformation("User logged in.");
                        return TypedResults.Redirect(returnUrl ?? "/");
                    }

                    if (result.RequiresTwoFactor)
                    {
                        return TypedResults.Redirect(
                            $"/Identity/Account/LoginWith2fa?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}&rememberMe={rememberMe}"
                        );
                    }

                    if (result.IsLockedOut)
                    {
                        logger.LogWarning("User account locked out.");
                        return TypedResults.Redirect("/Identity/Account/Lockout");
                    }

                    return Inertia.Render(
                        "Users/Account/Login",
                        new
                        {
                            returnUrl = returnUrl ?? "/",
                            showTestAccounts = false,
                            errors = new { email = "Invalid login attempt." },
                        }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
