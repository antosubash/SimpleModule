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

[ViewPage("Users/Account/ConfirmEmail")]
public class ConfirmEmailEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/ConfirmEmail",
                async (
                    [FromQuery] string? userId,
                    [FromQuery] string? code,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    if (userId is null || code is null)
                    {
                        return TypedResults.Redirect("/");
                    }

                    var user = await userManager.FindByIdAsync(userId);
                    if (user is null)
                    {
                        return Inertia.Render(
                            "Users/Account/ConfirmEmail",
                            new { message = "Error confirming your email." }
                        );
                    }

                    var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                    var result = await userManager.ConfirmEmailAsync(user, decodedCode);
                    var message = result.Succeeded
                        ? "Thank you for confirming your email."
                        : "Error confirming your email.";

                    return Inertia.Render("Users/Account/ConfirmEmail", new { message });
                }
            )
            .AllowAnonymous();
    }
}
