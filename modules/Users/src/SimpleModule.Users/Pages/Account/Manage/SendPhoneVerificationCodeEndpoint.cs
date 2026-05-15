using System.Globalization;
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

public class SendPhoneVerificationCodeEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.SendPhoneVerificationCode;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                Route,
                async (
                    [FromForm] string? phoneNumber,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    ISmsSender smsSender,
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
                    var currentPhoneNumber = await userManager.GetPhoneNumberAsync(user);
                    var isPhoneNumberConfirmed = await userManager.IsPhoneNumberConfirmedAsync(
                        user
                    );

                    if (string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        return Inertia.Render(
                            "Users/Account/Manage/Index",
                            new
                            {
                                username,
                                phoneNumber = currentPhoneNumber,
                                isPhoneNumberConfirmed,
                                statusMessage = "Error: Please enter a phone number.",
                            }
                        );
                    }

                    var userId = await userManager.GetUserIdAsync(user);
                    var slot = await throttle.TryAcquireResendSlotAsync(
                        userId,
                        VerificationChannel.Phone,
                        context.RequestAborted
                    );
                    if (!slot.Allowed)
                    {
                        var seconds = (int)Math.Ceiling((slot.RetryAfter ?? TimeSpan.Zero).TotalSeconds);
                        context.Response.Headers.RetryAfter = seconds.ToString(
                            CultureInfo.InvariantCulture
                        );
                        return Inertia.Render(
                            "Users/Account/Manage/Index",
                            new
                            {
                                username,
                                phoneNumber = currentPhoneNumber,
                                isPhoneNumberConfirmed,
                                pendingPhoneNumber = phoneNumber,
                                statusMessage = $"Please wait {seconds} seconds before requesting another code.",
                            }
                        );
                    }

                    var code = await userManager.GenerateChangePhoneNumberTokenAsync(
                        user,
                        phoneNumber
                    );
                    await smsSender.SendVerificationCodeAsync(user, phoneNumber, code);

                    return Inertia.Render(
                        "Users/Account/Manage/Index",
                        new
                        {
                            username,
                            phoneNumber = currentPhoneNumber,
                            isPhoneNumberConfirmed,
                            pendingPhoneNumber = phoneNumber,
                            statusMessage = "Verification code sent. Please check your phone.",
                        }
                    );
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
