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

namespace SimpleModule.Users.Pages.Account;

public class ResendEmailConfirmationEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/ResendEmailConfirmation",
                () => Inertia.Render("Users/Account/ResendEmailConfirmation")
            )
            .AllowAnonymous();

        app.MapPost(
                "/ResendEmailConfirmation",
                async (
                    [FromForm] string email,
                    UserManager<ApplicationUser> userManager,
                    IEmailSender<ApplicationUser> emailSender,
                    HttpContext context
                ) =>
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user is null)
                    {
                        return Inertia.Render(
                            "Users/Account/ResendEmailConfirmation",
                            new { message = "Verification email sent. Please check your email." }
                        );
                    }

                    var userId = await userManager.GetUserIdAsync(user);
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var request = context.Request;
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    var callbackUrl =
                        $"{baseUrl}/Identity/Account/ConfirmEmail?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}";

                    await emailSender.SendConfirmationLinkAsync(user, email, callbackUrl);

                    return Inertia.Render(
                        "Users/Account/ResendEmailConfirmation",
                        new { message = "Verification email sent. Please check your email." }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
