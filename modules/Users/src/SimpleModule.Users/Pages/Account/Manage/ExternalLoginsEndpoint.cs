using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account.Manage;

public class ExternalLoginsEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.ExternalLogins;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (
                    [FromQuery] string? action,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager,
                    HttpContext context
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    string? statusMessage = null;

                    if (action == "LinkLoginCallback")
                    {
                        var userId = await userManager.GetUserIdAsync(user);
                        var info = await signInManager.GetExternalLoginInfoAsync(userId);
                        if (info is null)
                        {
                            throw new InvalidOperationException(
                                "Unexpected error occurred loading external login info."
                            );
                        }

                        var addResult = await userManager.AddLoginAsync(user, info);
                        if (!addResult.Succeeded)
                        {
                            statusMessage =
                                "Error: The external login was not added. External logins can only be associated with one account.";
                        }
                        else
                        {
                            await context.SignOutAsync(IdentityConstants.ExternalScheme);
                            statusMessage = "The external login was added.";
                        }
                    }

                    var currentLogins = await userManager.GetLoginsAsync(user);
                    var otherLogins = (await signInManager.GetExternalAuthenticationSchemesAsync())
                        .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
                        .Select(s => new { name = s.Name, displayName = s.DisplayName })
                        .ToArray();

                    var hasPassword = await userManager.HasPasswordAsync(user);
                    var showRemoveButton = hasPassword || currentLogins.Count > 1;

                    return Inertia.Render(
                        "Users/Account/Manage/ExternalLogins",
                        new
                        {
                            currentLogins = currentLogins
                                .Select(l => new
                                {
                                    loginProvider = l.LoginProvider,
                                    providerKey = l.ProviderKey,
                                    providerDisplayName = l.ProviderDisplayName,
                                })
                                .ToArray(),
                            otherLogins,
                            showRemoveButton,
                            statusMessage,
                        }
                    );
                }
            )
            .RequireAuthorization();

        app.MapPost(
                Route,
                async (
                    HttpContext context,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    var formAction = context.Request.Form["formAction"].ToString();
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    if (formAction == "remove")
                    {
                        var loginProvider = context.Request.Form["loginProvider"].ToString();
                        var providerKey = context.Request.Form["providerKey"].ToString();

                        var result = await userManager.RemoveLoginAsync(
                            user,
                            loginProvider,
                            providerKey
                        );
                        if (!result.Succeeded)
                        {
                            return TypedResults.Redirect("/Identity/Account/Manage/ExternalLogins");
                        }

                        await signInManager.RefreshSignInAsync(user);
                        return TypedResults.Redirect("/Identity/Account/Manage/ExternalLogins");
                    }

                    // Link login
                    var provider = context.Request.Form["provider"].ToString();
                    await context.SignOutAsync(IdentityConstants.ExternalScheme);

                    var request = context.Request;
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    var redirectUrl =
                        $"{baseUrl}/Identity/Account/Manage/ExternalLogins?action=LinkLoginCallback";
                    var properties = signInManager.ConfigureExternalAuthenticationProperties(
                        provider,
                        redirectUrl,
                        userManager.GetUserId(principal)
                    );
                    return Results.Challenge(properties, [provider]);
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
