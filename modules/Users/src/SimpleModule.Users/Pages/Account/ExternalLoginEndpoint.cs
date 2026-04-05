using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
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

public class ExternalLoginEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/ExternalLogin",
                async (
                    [FromQuery] string? action,
                    [FromQuery] string? returnUrl,
                    [FromQuery] string? remoteError,
                    SignInManager<ApplicationUser> signInManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    returnUrl ??= "/";

                    if (action != "Callback")
                    {
                        return TypedResults.Redirect("/Identity/Account/Login");
                    }

                    if (remoteError is not null)
                    {
                        return TypedResults.Redirect(
                            $"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}"
                        );
                    }

                    var info = await signInManager.GetExternalLoginInfoAsync();
                    if (info is null)
                    {
                        return TypedResults.Redirect(
                            $"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}"
                        );
                    }

                    var result = await signInManager.ExternalLoginSignInAsync(
                        info.LoginProvider,
                        info.ProviderKey,
                        isPersistent: false,
                        bypassTwoFactor: true
                    );

                    if (result.Succeeded)
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation(
                                "{Name} logged in with {LoginProvider} provider.",
                                info.Principal.Identity?.Name,
                                info.LoginProvider
                            );
                        }
                        return TypedResults.Redirect(returnUrl);
                    }

                    if (result.IsLockedOut)
                    {
                        return TypedResults.Redirect("/Identity/Account/Lockout");
                    }

                    var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
                    return Inertia.Render(
                        "Users/Account/ExternalLogin",
                        new
                        {
                            returnUrl,
                            providerDisplayName = info.ProviderDisplayName ?? info.LoginProvider,
                            email,
                        }
                    );
                }
            )
            .AllowAnonymous();

        app.MapPost(
                "/ExternalLogin",
                async (
                    HttpContext context,
                    [FromQuery] string? action,
                    SignInManager<ApplicationUser> signInManager,
                    UserManager<ApplicationUser> userManager,
                    IUserStore<ApplicationUser> userStore,
                    IEmailSender<ApplicationUser> emailSender,
                    ILogger<UsersModule> logger
                ) =>
                {
                    if (action is null)
                    {
                        var provider = context.Request.Form["provider"].ToString();
                        var returnUrl = context.Request.Form["returnUrl"].ToString();
                        if (string.IsNullOrEmpty(returnUrl))
                            returnUrl = "/";

                        var request = context.Request;
                        var baseUrl = $"{request.Scheme}://{request.Host}";
                        var redirectUrl =
                            $"{baseUrl}/Identity/Account/ExternalLogin?action=Callback&returnUrl={Uri.EscapeDataString(returnUrl)}";
                        var properties = signInManager.ConfigureExternalAuthenticationProperties(
                            provider,
                            redirectUrl
                        );
                        return Results.Challenge(properties, [provider]);
                    }

                    var email = context.Request.Form["email"].ToString();
                    var retUrl = context.Request.Form["returnUrl"].ToString();
                    if (string.IsNullOrEmpty(retUrl))
                        retUrl = "/";

                    var info = await signInManager.GetExternalLoginInfoAsync();
                    if (info is null)
                    {
                        return TypedResults.Redirect(
                            $"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(retUrl)}"
                        );
                    }

                    var user = new ApplicationUser();
                    await userStore.SetUserNameAsync(user, email, CancellationToken.None);
                    var emailStore = (IUserEmailStore<ApplicationUser>)userStore;
                    await emailStore.SetEmailAsync(user, email, CancellationToken.None);

                    var result = await userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddLoginAsync(user, info);
                        if (result.Succeeded)
                        {
                            if (logger.IsEnabled(LogLevel.Information))
                            {
                                logger.LogInformation(
                                    "User created an account using {Name} provider.",
                                    info.LoginProvider
                                );
                            }

                            var userId = await userManager.GetUserIdAsync(user);
                            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                            var req = context.Request;
                            var bUrl = $"{req.Scheme}://{req.Host}";
                            var callbackUrl =
                                $"{bUrl}/Identity/Account/ConfirmEmail?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}";

                            await emailSender.SendConfirmationLinkAsync(user, email, callbackUrl);

                            if (userManager.Options.SignIn.RequireConfirmedAccount)
                            {
                                return TypedResults.Redirect(
                                    $"/Identity/Account/RegisterConfirmation?email={Uri.EscapeDataString(email)}"
                                );
                            }

                            await signInManager.SignInAsync(
                                user,
                                isPersistent: false,
                                info.LoginProvider
                            );
                            return TypedResults.Redirect(retUrl);
                        }
                    }

                    return Inertia.Render(
                        "Users/Account/ExternalLogin",
                        new
                        {
                            returnUrl = retUrl,
                            providerDisplayName = info.ProviderDisplayName ?? info.LoginProvider,
                            email,
                            errors = result.Errors.Select(e => e.Description).ToArray(),
                        }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery();
    }
}
