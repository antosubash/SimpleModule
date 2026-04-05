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

[ViewPage("Users/Account/RegisterConfirmation")]
public class RegisterConfirmationEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/RegisterConfirmation",
                async (
                    [FromQuery] string? email,
                    [FromQuery] string? returnUrl,
                    UserManager<ApplicationUser> userManager,
                    HttpContext context
                ) =>
                {
                    if (email is null)
                    {
                        return TypedResults.Redirect("/");
                    }

                    var user = await userManager.FindByEmailAsync(email);
                    if (user is null)
                    {
                        return Inertia.Render(
                            "Users/Account/RegisterConfirmation",
                            new
                            {
                                email,
                                displayConfirmAccountLink = false,
                                emailConfirmationUrl = (string?)null,
                            }
                        );
                    }

                    // Once you add a real email sender, remove the direct link
                    var userId = await userManager.GetUserIdAsync(user);
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var request = context.Request;
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    var callbackUrl =
                        $"{baseUrl}/Identity/Account/ConfirmEmail?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";

                    return Inertia.Render(
                        "Users/Account/RegisterConfirmation",
                        new
                        {
                            email,
                            displayConfirmAccountLink = true,
                            emailConfirmationUrl = callbackUrl,
                        }
                    );
                }
            )
            .AllowAnonymous();
    }
}
