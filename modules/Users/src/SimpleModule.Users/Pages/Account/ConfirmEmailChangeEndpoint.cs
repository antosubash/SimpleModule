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

public class ConfirmEmailChangeEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/ConfirmEmailChange",
                async (
                    [FromQuery] string? userId,
                    [FromQuery] string? email,
                    [FromQuery] string? code,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    if (userId is null || email is null || code is null)
                    {
                        return TypedResults.Redirect("/");
                    }

                    var user = await userManager.FindByIdAsync(userId);
                    if (user is null)
                    {
                        return Inertia.Render(
                            "Users/Account/ConfirmEmailChange",
                            new { message = "Error changing email." }
                        );
                    }

                    var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                    var result = await userManager.ChangeEmailAsync(user, email, decodedCode);
                    if (!result.Succeeded)
                    {
                        return Inertia.Render(
                            "Users/Account/ConfirmEmailChange",
                            new { message = "Error changing email." }
                        );
                    }

                    var setUserNameResult = await userManager.SetUserNameAsync(user, email);
                    if (!setUserNameResult.Succeeded)
                    {
                        return Inertia.Render(
                            "Users/Account/ConfirmEmailChange",
                            new { message = "Error changing user name." }
                        );
                    }

                    await signInManager.RefreshSignInAsync(user);
                    return Inertia.Render(
                        "Users/Account/ConfirmEmailChange",
                        new { message = "Thank you for confirming your email change." }
                    );
                }
            )
            .AllowAnonymous();
    }
}
