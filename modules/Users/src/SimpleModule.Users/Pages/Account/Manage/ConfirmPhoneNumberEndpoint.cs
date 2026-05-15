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

public class ConfirmPhoneNumberEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.ConfirmPhoneNumber;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                Route,
                async (
                    [FromForm] string? phoneNumber,
                    [FromForm] string? code,
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

                    if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(code))
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
                                pendingPhoneNumber = phoneNumber,
                                statusMessage = "Error: Phone number and verification code are required.",
                            }
                        );
                    }

                    var result = await userManager.ChangePhoneNumberAsync(user, phoneNumber, code);
                    if (!result.Succeeded)
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
                                pendingPhoneNumber = phoneNumber,
                                statusMessage = "Error: Invalid or expired verification code.",
                            }
                        );
                    }

                    await signInManager.RefreshSignInAsync(user);

                    return Inertia.Render(
                        "Users/Account/Manage/Index",
                        new
                        {
                            username,
                            phoneNumber = await userManager.GetPhoneNumberAsync(user),
                            isPhoneNumberConfirmed = await userManager.IsPhoneNumberConfirmedAsync(
                                user
                            ),
                            statusMessage = "Your phone number has been verified.",
                        }
                    );
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
