using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.RateLimiting;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class SendUnlockEmailEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.SendUnlockEmail;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Users/Account/SendUnlockEmail")).AllowAnonymous();

        app.MapPost(
                Route,
                async (
                    [FromForm] string email,
                    UserManager<ApplicationUser> userManager,
                    IAccountUnlockEmailSender unlockEmailSender,
                    HttpContext context
                ) =>
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (
                        user is not null
                        && await userManager.IsEmailConfirmedAsync(user)
                        && await userManager.IsLockedOutAsync(user)
                    )
                    {
                        var code = await userManager.GenerateUserTokenAsync(
                            user,
                            TokenOptions.DefaultProvider,
                            UsersConstants.TokenPurposes.AccountUnlock
                        );
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        var request = context.Request;
                        var baseUrl = $"{request.Scheme}://{request.Host}";
                        var callbackUrl =
                            $"{baseUrl}{UsersConstants.ViewPrefix}{UsersConstants.Routes.UnlockAccount}?userId={Uri.EscapeDataString(user.Id)}&code={Uri.EscapeDataString(code)}";

                        await unlockEmailSender.SendUnlockLinkAsync(email, callbackUrl);
                    }

                    // Always redirect to confirmation — don't reveal whether the email exists or is locked
                    return TypedResults.Redirect(
                        $"{UsersConstants.ViewPrefix}{UsersConstants.Routes.SendUnlockEmailConfirmation}"
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery()
            .RateLimit(RateLimitPolicies.AuthStrict);
    }
}
