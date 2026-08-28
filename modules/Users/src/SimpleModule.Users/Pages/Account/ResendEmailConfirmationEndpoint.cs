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
using SimpleModule.Users.Services;

namespace SimpleModule.Users.Pages.Account;

public class ResendEmailConfirmationEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.ResendEmailConfirmation;

    private const string GenericMessage = "Verification email sent. Please check your email.";

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Users/Account/ResendEmailConfirmation"))
            .AllowAnonymous();

        app.MapPost(
                Route,
                async (
                    [FromForm] string email,
                    UserManager<ApplicationUser> userManager,
                    IEmailSender<ApplicationUser> emailSender,
                    IVerificationThrottle throttle,
                    HttpContext context
                ) =>
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user is null)
                    {
                        // No user — still respond identically so we don't leak
                        // which addresses are registered, but skip the SMS/email
                        // dispatch entirely.
                        return Inertia.Render(
                            "Users/Account/ResendEmailConfirmation",
                            new { message = GenericMessage }
                        );
                    }

                    var userId = await userManager.GetUserIdAsync(user);
                    var slot = await throttle.TryAcquireResendSlotAsync(
                        userId,
                        VerificationChannel.Email,
                        context.RequestAborted
                    );
                    if (!slot.Allowed)
                    {
                        // Reuse the same opaque response shape, but tell the
                        // caller (and the audit log) how long to wait.
                        var seconds = (int)Math.Ceiling((slot.RetryAfter ?? TimeSpan.Zero).TotalSeconds);
                        context.Response.Headers.RetryAfter = seconds.ToString(
                            System.Globalization.CultureInfo.InvariantCulture
                        );
                        return Inertia.Render(
                            "Users/Account/ResendEmailConfirmation",
                            new { message = GenericMessage }
                        );
                    }

                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var request = context.Request;
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    var callbackUrl =
                        $"{baseUrl}/Identity/Account/ConfirmEmail?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}";

                    await emailSender.SendConfirmationLinkAsync(user, email, callbackUrl);

                    return Inertia.Render(
                        "Users/Account/ResendEmailConfirmation",
                        new { message = GenericMessage }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
