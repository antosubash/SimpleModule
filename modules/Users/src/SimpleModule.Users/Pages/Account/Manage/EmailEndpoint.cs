using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account.Manage;

public class EmailEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.Email;

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

                    var email = await userManager.GetEmailAsync(user);
                    var isEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);

                    return Inertia.Render(
                        "Users/Account/Manage/Email",
                        new
                        {
                            email,
                            isEmailConfirmed,
                            newEmail = email,
                        }
                    );
                }
            )
            .RequireAuthorization();

        app.MapPost(
                Route,
                async (
                    [FromForm] string newEmail,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    IEmailSender<ApplicationUser> emailSender,
                    HttpContext context
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    var email = await userManager.GetEmailAsync(user);
                    var isEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);

                    if (newEmail != email)
                    {
                        var userId = await userManager.GetUserIdAsync(user);
                        var code = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var request = context.Request;
                        var baseUrl = $"{request.Scheme}://{request.Host}";
                        var callbackUrl =
                            $"{baseUrl}/Identity/Account/ConfirmEmailChange?userId={Uri.EscapeDataString(userId)}&email={Uri.EscapeDataString(newEmail)}&code={Uri.EscapeDataString(code)}";

                        await emailSender.SendConfirmationLinkAsync(user, newEmail, callbackUrl);

                        return Inertia.Render(
                            "Users/Account/Manage/Email",
                            new
                            {
                                email,
                                isEmailConfirmed,
                                newEmail,
                                statusMessage = "Confirmation link to change email sent. Please check your email.",
                            }
                        );
                    }

                    return Inertia.Render(
                        "Users/Account/Manage/Email",
                        new
                        {
                            email,
                            isEmailConfirmed,
                            newEmail = email,
                            statusMessage = "Your email is unchanged.",
                        }
                    );
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery();
    }
}
