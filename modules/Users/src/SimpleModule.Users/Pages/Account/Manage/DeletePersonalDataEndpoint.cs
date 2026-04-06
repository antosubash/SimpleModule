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

public class DeletePersonalDataEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.DeletePersonalData;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    var requirePassword = await userManager.HasPasswordAsync(user);
                    return Inertia.Render(
                        "Users/Account/Manage/DeletePersonalData",
                        new { requirePassword }
                    );
                }
            )
            .RequireAuthorization();

        app.MapPost(
                Route,
                async (
                    [FromForm] string? password,
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

                    var requirePassword = await userManager.HasPasswordAsync(user);
                    if (requirePassword)
                    {
                        if (!await userManager.CheckPasswordAsync(user, password ?? ""))
                        {
                            return Inertia.Render(
                                "Users/Account/Manage/DeletePersonalData",
                                new
                                {
                                    requirePassword = true,
                                    errors = (string[])["Incorrect password."],
                                }
                            );
                        }
                    }

                    var result = await userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Unexpected error occurred deleting user."
                        );
                    }

                    await signInManager.SignOutAsync();
                    logger.LogInformation("User deleted themselves.");
                    return TypedResults.Redirect("/");
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
