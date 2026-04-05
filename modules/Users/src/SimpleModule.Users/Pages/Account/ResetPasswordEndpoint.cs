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

public class ResetPasswordEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/ResetPassword",
                ([FromQuery] string? code) =>
                {
                    if (code is null)
                    {
                        return Inertia.Render(
                            "Users/Account/ResetPassword",
                            new { invalidCode = true, code = (string?)null }
                        );
                    }

                    var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                    return Inertia.Render(
                        "Users/Account/ResetPassword",
                        new { invalidCode = false, code = decodedCode }
                    );
                }
            )
            .AllowAnonymous();

        app.MapPost(
                "/ResetPassword",
                async (
                    [FromForm] string email,
                    [FromForm] string password,
                    [FromForm] string confirmPassword,
                    [FromForm] string code,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user is null)
                    {
                        return TypedResults.Redirect("/Identity/Account/ResetPasswordConfirmation");
                    }

                    var result = await userManager.ResetPasswordAsync(user, code, password);
                    if (result.Succeeded)
                    {
                        return TypedResults.Redirect("/Identity/Account/ResetPasswordConfirmation");
                    }

                    return Inertia.Render(
                        "Users/Account/ResetPassword",
                        new
                        {
                            invalidCode = false,
                            code,
                            errors = result.Errors.Select(e => e.Description).ToArray(),
                        }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
