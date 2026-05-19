using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.RateLimiting;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public partial class LoginEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.Login;

    [LoggerMessage(Level = LogLevel.Information, Message = "User logged in.")]
    private static partial void LogUserLoggedIn(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User account locked out.")]
    private static partial void LogUserLockedOut(ILogger logger);

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (
                    HttpContext context,
                    ISettingsContracts settingsService,
                    ISettingsDefinitionRegistry settingsDefinitions,
                    IOptions<IdentityPasskeyOptions> passkeyOptions,
                    [FromQuery] string? returnUrl,
                    [FromQuery] bool? signedOutEverywhere
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
                            passkeyEnabled = !string.IsNullOrEmpty(
                                passkeyOptions.Value.ServerDomain
                            ),
                            signedOutEverywhere = signedOutEverywhere == true,
                        }
                    );
                }
            )
            .AllowAnonymous();

        app.MapPost(
                Route,
                async (
                    [FromForm] string email,
                    [FromForm] string password,
                    [FromForm] bool? rememberMe,
                    [FromQuery] string? returnUrl,
                    SignInManager<ApplicationUser> signInManager,
                    ILogger<UsersModule> logger,
                    IOptions<IdentityPasskeyOptions> passkeyOptions
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
                        LogUserLoggedIn(logger);
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
                        LogUserLockedOut(logger);
                        return TypedResults.Redirect("/Identity/Account/Lockout");
                    }

                    return Inertia.Render(
                        "Users/Account/Login",
                        new
                        {
                            returnUrl = returnUrl ?? "/",
                            showTestAccounts = false,
                            passkeyEnabled = !string.IsNullOrEmpty(
                                passkeyOptions.Value.ServerDomain
                            ),
                            errors = new { email = "Invalid login attempt." },
                        }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery()
            .RateLimit(RateLimitPolicies.AuthStrict);
    }
}
