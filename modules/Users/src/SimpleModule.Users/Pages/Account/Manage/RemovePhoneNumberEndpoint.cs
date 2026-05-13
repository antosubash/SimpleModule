using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account.Manage;

public class RemovePhoneNumberEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.RemovePhoneNumber;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                Route,
                async (
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

                    var username = await userManager.GetUserNameAsync(user);
                    var setResult = await userManager.SetPhoneNumberAsync(user, null);
                    if (!setResult.Succeeded)
                    {
                        return Inertia.Render(
                            "Users/Account/Manage/Index",
                            new
                            {
                                username,
                                phoneNumber = await userManager.GetPhoneNumberAsync(user),
                                isPhoneNumberConfirmed = await userManager.IsPhoneNumberConfirmedAsync(
                                    user
                                ),
                                statusMessage = "Error: Unable to remove phone number.",
                            }
                        );
                    }

                    await signInManager.RefreshSignInAsync(user);

                    return Inertia.Render(
                        "Users/Account/Manage/Index",
                        new
                        {
                            username,
                            phoneNumber = (string?)null,
                            isPhoneNumberConfirmed = false,
                            statusMessage = "Your phone number has been removed.",
                        }
                    );
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
