using System.Buffers;
using System.Buffers.Text;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Contracts.Events;
using Wolverine;

namespace SimpleModule.Users.Pages.Account;

public class UnlockAccountEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.UnlockAccount;

    private const string ComponentName = "Users/Account/UnlockAccount";

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

                    static IResult InvalidLink() =>
                        Inertia.Render(
                            ComponentName,
                            new { success = false, message = "Invalid or expired unlock link." }
                        );

                    var user = await userManager.FindByIdAsync(userId);
                    if (user is null)
                    {
                        return InvalidLink();
                    }

                    var decodeBuffer = new byte[Base64Url.GetMaxDecodedLength(code.Length)];
                    var decodeStatus = Base64Url.DecodeFromChars(
                        code,
                        decodeBuffer,
                        out _,
                        out var bytesWritten,
                        isFinalBlock: true
                    );
                    if (decodeStatus != OperationStatus.Done)
                    {
                        return InvalidLink();
                    }
                    var decodedCode = Encoding.UTF8.GetString(decodeBuffer, 0, bytesWritten);

                    var isValid = await userManager.VerifyUserTokenAsync(
                        user,
                        TokenOptions.DefaultProvider,
                        UsersConstants.TokenPurposes.AccountUnlock,
                        decodedCode
                    );

                    if (!isValid)
                    {
                        return InvalidLink();
                    }

                    user.LockoutEnd = null;
                    user.AccessFailedCount = 0;
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        return InvalidLink();
                    }
                    var stampResult = await userManager.UpdateSecurityStampAsync(user);
                    if (!stampResult.Succeeded)
                    {
                        return InvalidLink();
                    }

                    await bus.PublishAsync(
                        new UserSelfUnlockedEvent(UserId.From(user.Id), user.Email ?? string.Empty)
                    );

                    return Inertia.Render(
                        ComponentName,
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
