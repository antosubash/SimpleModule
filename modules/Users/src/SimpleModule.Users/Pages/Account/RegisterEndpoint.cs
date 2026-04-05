using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class RegisterEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.Register;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (
                    SignInManager<ApplicationUser> signInManager,
                    [FromQuery] string? returnUrl
                ) =>
                {
                    var externalLogins = (
                        await signInManager.GetExternalAuthenticationSchemesAsync()
                    )
                        .Select(s => new { name = s.Name, displayName = s.DisplayName })
                        .ToArray();

                    return Inertia.Render(
                        "Users/Account/Register",
                        new { returnUrl = returnUrl ?? "/", externalLogins }
                    );
                }
            )
            .AllowAnonymous();

        app.MapPost(
                Route,
                async (
                    [FromForm] string email,
                    [FromForm] string password,
                    [FromForm] string confirmPassword,
                    [FromQuery] string? returnUrl,
                    UserManager<ApplicationUser> userManager,
                    IUserStore<ApplicationUser> userStore,
                    SignInManager<ApplicationUser> signInManager,
                    IEmailSender<ApplicationUser> emailSender,
                    HttpContext context,
                    ILogger<UsersModule> logger
                ) =>
                {
                    returnUrl ??= "/";

                    if (password != confirmPassword)
                    {
                        return Inertia.Render(
                            "Users/Account/Register",
                            new
                            {
                                returnUrl,
                                externalLogins = Array.Empty<object>(),
                                errors = (string[])
                                    ["The password and confirmation password do not match."],
                            }
                        );
                    }

                    var user = new ApplicationUser();
                    await userStore.SetUserNameAsync(user, email, CancellationToken.None);
                    var emailStore = (IUserEmailStore<ApplicationUser>)userStore;
                    await emailStore.SetEmailAsync(user, email, CancellationToken.None);
                    var result = await userManager.CreateAsync(user, password);

                    if (!result.Succeeded)
                    {
                        return Inertia.Render(
                            "Users/Account/Register",
                            new
                            {
                                returnUrl,
                                externalLogins = Array.Empty<object>(),
                                errors = result.Errors.Select(e => e.Description).ToArray(),
                            }
                        );
                    }

                    logger.LogInformation("User created a new account with password.");

                    var userId = await userManager.GetUserIdAsync(user);
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var request = context.Request;
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    var callbackUrl =
                        $"{baseUrl}/Identity/Account/ConfirmEmail?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}&returnUrl={Uri.EscapeDataString(returnUrl)}";

                    await emailSender.SendConfirmationLinkAsync(user, email, callbackUrl);

                    if (userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return TypedResults.Redirect(
                            $"/Identity/Account/RegisterConfirmation?email={Uri.EscapeDataString(email)}&returnUrl={Uri.EscapeDataString(returnUrl)}"
                        );
                    }

                    await signInManager.SignInAsync(user, isPersistent: false);
                    return TypedResults.Redirect(returnUrl);
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
