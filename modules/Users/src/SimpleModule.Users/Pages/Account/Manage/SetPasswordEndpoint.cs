using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account.Manage;

public class SetPasswordEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Manage/SetPassword",
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    var hasPassword = await userManager.HasPasswordAsync(user);
                    if (hasPassword)
                    {
                        return TypedResults.Redirect("/Identity/Account/Manage/ChangePassword");
                    }

                    return Inertia.Render("Users/Account/Manage/SetPassword");
                }
            )
            .RequireAuthorization();

        app.MapPost(
                "/Manage/SetPassword",
                async (
                    [FromForm] string newPassword,
                    [FromForm] string confirmPassword,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    var addPasswordResult = await userManager.AddPasswordAsync(user, newPassword);
                    if (!addPasswordResult.Succeeded)
                    {
                        return Inertia.Render(
                            "Users/Account/Manage/SetPassword",
                            new
                            {
                                errors = addPasswordResult
                                    .Errors.Select(e => e.Description)
                                    .ToArray(),
                            }
                        );
                    }

                    await signInManager.RefreshSignInAsync(user);
                    return Inertia.Render(
                        "Users/Account/Manage/SetPassword",
                        new { statusMessage = "Your password has been set." }
                    );
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
