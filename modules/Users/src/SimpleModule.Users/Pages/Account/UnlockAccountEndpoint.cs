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
using SimpleModule.Users.Contracts.Events;
using Wolverine;

namespace SimpleModule.Users.Pages.Account;

public class UnlockAccountEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.UnlockAccount;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async Task<IResult> (
                    [FromQuery] string? userId,
                    [FromQuery] string? code,
                    UserManager<ApplicationUser> userManager,
                    IMessageBus bus
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
                            "Users/Account/UnlockAccount",
                            new { success = false, message = "Unable to unlock account." }
                        );
                    }

                    var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                    var isValid = await userManager.VerifyUserTokenAsync(
                        user,
                        TokenOptions.DefaultProvider,
                        "AccountUnlock",
                        decodedCode
                    );

                    if (!isValid)
                    {
                        return Inertia.Render(
                            "Users/Account/UnlockAccount",
                            new
                            {
                                success = false,
                                message = "Invalid or expired unlock link.",
                            }
                        );
                    }

                    user.LockoutEnd = null;
                    user.AccessFailedCount = 0;
                    await userManager.UpdateAsync(user);
                    await userManager.UpdateSecurityStampAsync(user);

                    await bus.PublishAsync(
                        new UserSelfUnlockedEvent(
                            UserId.From(user.Id),
                            user.Email ?? string.Empty
                        )
                    );

                    return Inertia.Render(
                        "Users/Account/UnlockAccount",
                        new
                        {
                            success = true,
                            message = "Your account has been unlocked successfully.",
                        }
                    );
                }
            )
            .AllowAnonymous();
    }
}
