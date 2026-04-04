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

namespace SimpleModule.Users.Views.Account;

[ViewPage("Users/Account/ForgotPassword")]
public class ForgotPasswordEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/ForgotPassword", () => Inertia.Render("Users/Account/ForgotPassword"))
            .AllowAnonymous();

        app.MapPost(
                "/ForgotPassword",
                async (
                    [FromForm] string email,
                    UserManager<ApplicationUser> userManager,
                    IEmailSender<ApplicationUser> emailSender,
                    HttpContext context
                ) =>
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user is null || !await userManager.IsEmailConfirmedAsync(user))
                    {
                        return TypedResults.Redirect(
                            "/Identity/Account/ForgotPasswordConfirmation"
                        );
                    }

                    var code = await userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var request = context.Request;
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    var callbackUrl =
                        $"{baseUrl}/Identity/Account/ResetPassword?code={Uri.EscapeDataString(code)}";

                    await emailSender.SendPasswordResetLinkAsync(user, email, callbackUrl);

                    return TypedResults.Redirect("/Identity/Account/ForgotPasswordConfirmation");
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
