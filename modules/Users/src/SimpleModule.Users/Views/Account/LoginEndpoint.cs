using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Views.Account;

public class LoginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Identity/Account/Login")
            .WithTags(UsersConstants.ModuleName)
            .ExcludeFromDescription();

        group
            .MapGet(
                "",
                async (
                    HttpContext context,
                    [FromQuery] string? returnUrl,
                    ISettingsContracts settings,
                    ISettingsDefinitionRegistry settingsDefinitions
                ) =>
                {
                    await context.SignOutAsync(IdentityConstants.ExternalScheme);

                    var value = await settings.GetSettingAsync(
                        ConfigKeys.ShowTestAccounts,
                        SettingScope.System
                    );
                    value ??= settingsDefinitions
                        .GetDefinition(ConfigKeys.ShowTestAccounts)
                        ?.DefaultValue;
                    var showTestAccounts = value is "true";

                    return Inertia.Render(
                        "Users/Account/Login",
                        new { returnUrl, showTestAccounts }
                    );
                }
            )
            .AllowAnonymous();

        group
            .MapPost(
                "",
                async (
                    [FromForm] string email,
                    [FromForm] string password,
                    [FromForm] bool rememberMe,
                    [FromQuery] string? returnUrl,
                    SignInManager<ApplicationUser> signInManager,
                    ISettingsContracts settings,
                    ISettingsDefinitionRegistry settingsDefinitions
                ) =>
                {
                    var result = await signInManager.PasswordSignInAsync(
                        email,
                        password,
                        rememberMe,
                        lockoutOnFailure: false
                    );

                    if (result.Succeeded)
                        return TypedResults.Redirect(returnUrl ?? "/") as IResult;

                    if (result.RequiresTwoFactor)
                        return TypedResults.Redirect(
                            $"/Identity/Account/LoginWith2fa?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}&rememberMe={rememberMe}"
                        );

                    if (result.IsLockedOut)
                        return TypedResults.Redirect("/Identity/Account/Lockout");

                    var value = await settings.GetSettingAsync(
                        ConfigKeys.ShowTestAccounts,
                        SettingScope.System
                    );
                    value ??= settingsDefinitions
                        .GetDefinition(ConfigKeys.ShowTestAccounts)
                        ?.DefaultValue;
                    var showTestAccounts = value is "true";

                    return Inertia.Render(
                        "Users/Account/Login",
                        new
                        {
                            returnUrl,
                            showTestAccounts,
                            errors = new { email = "Invalid login attempt." },
                        }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
