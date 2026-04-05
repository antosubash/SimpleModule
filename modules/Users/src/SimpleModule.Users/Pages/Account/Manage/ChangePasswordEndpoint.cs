using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account.Manage;

public class ChangePasswordEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Manage/ChangePassword",
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    var hasPassword = await userManager.HasPasswordAsync(user);
                    if (!hasPassword)
                    {
                        return TypedResults.Redirect("/Identity/Account/Manage/SetPassword");
                    }

                    return Inertia.Render("Users/Account/Manage/ChangePassword");
                }
            )
            .RequireAuthorization();

        app.MapPost(
                "/Manage/ChangePassword",
                async (
                    [FromForm] string oldPassword,
                    [FromForm] string newPassword,
                    [FromForm] string confirmPassword,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    var changeResult = await userManager.ChangePasswordAsync(
                        user,
                        oldPassword,
                        newPassword
                    );
                    if (!changeResult.Succeeded)
                    {
                        return Inertia.Render(
                            "Users/Account/Manage/ChangePassword",
                            new
                            {
                                errors = changeResult.Errors.Select(e => e.Description).ToArray(),
                            }
                        );
                    }

                    await signInManager.RefreshSignInAsync(user);
                    logger.LogInformation("User changed their password successfully.");
                    return Inertia.Render(
                        "Users/Account/Manage/ChangePassword",
                        new { statusMessage = "Your password has been changed." }
                    );
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
