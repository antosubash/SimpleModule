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

public class ManageIndexEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.ManageIndex;

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

                    var username = await userManager.GetUserNameAsync(user);
                    var phoneNumber = await userManager.GetPhoneNumberAsync(user);

                    return Inertia.Render(
                        "Users/Account/Manage/Index",
                        new { username, phoneNumber }
                    );
                }
            )
            .RequireAuthorization();

        app.MapPost(
                Route,
                async (
                    [FromForm] string? phoneNumber,
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

                    var currentPhone = await userManager.GetPhoneNumberAsync(user);
                    if (phoneNumber != currentPhone)
                    {
                        var setPhoneResult = await userManager.SetPhoneNumberAsync(
                            user,
                            phoneNumber
                        );
                        if (!setPhoneResult.Succeeded)
                        {
                            return Inertia.Render(
                                "Users/Account/Manage/Index",
                                new
                                {
                                    username = await userManager.GetUserNameAsync(user),
                                    phoneNumber = currentPhone,
                                    statusMessage = "Error: Unexpected error when trying to set phone number.",
                                }
                            );
                        }
                    }

                    await signInManager.RefreshSignInAsync(user);
                    return Inertia.Render(
                        "Users/Account/Manage/Index",
                        new
                        {
                            username = await userManager.GetUserNameAsync(user),
                            phoneNumber = await userManager.GetPhoneNumberAsync(user),
                            statusMessage = "Your profile has been updated",
                        }
                    );
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
