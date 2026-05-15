using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Services;

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
                    SignInManager<ApplicationUser> signInManager,
                    IVerificationThrottle throttle,
                    HttpContext context
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

                    var userId = await userManager.GetUserIdAsync(user);
                    if (await throttle.IsLockedOutAsync(userId, VerificationChannel.Phone, context.RequestAborted))
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
                                statusMessage = "Too many failed attempts. Please try again later.",
                            }
                        );
                    }

                    var result = await userManager.ChangePhoneNumberAsync(user, phoneNumber, code);
                    if (!result.Succeeded)
                    {
                        var attempt = await throttle.RecordAttemptAsync(
                            userId,
                            VerificationChannel.Phone,
                            succeeded: false,
                            context.RequestAborted
                        );
                        var msg = attempt.LockedOut
                            ? "Too many failed attempts. Please try again later."
                            : "Error: Invalid or expired verification code.";
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
                                statusMessage = msg,
                            }
                        );
                    }

                    await throttle.RecordAttemptAsync(
                        userId,
                        VerificationChannel.Phone,
                        succeeded: true,
                        context.RequestAborted
                    );
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
