using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account;

public class EnableAuthenticatorEndpoint : IViewEndpoint
{
    public const string Route = UsersConstants.Routes.EnableAuthenticator;

    private static readonly CompositeFormat AuthenticatorUriFormat = CompositeFormat.Parse(
        "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6"
    );

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    var (sharedKey, authenticatorUri) = await LoadSharedKeyAndQrCodeUriAsync(
                        userManager,
                        user
                    );

                    return Inertia.Render(
                        "Users/Account/EnableAuthenticator",
                        new { sharedKey, authenticatorUri }
                    );
                }
            )
            .RequireAuthorization();
    }

    internal static async Task<(
        string sharedKey,
        string authenticatorUri
    )> LoadSharedKeyAndQrCodeUriAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user
    )
    {
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var sharedKey = FormatKey(unformattedKey!);
        var email = await userManager.GetEmailAsync(user);
        var authenticatorUri = string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            UrlEncoder.Default.Encode("SimpleModule"),
            UrlEncoder.Default.Encode(email!),
            unformattedKey
        );

        return (sharedKey, authenticatorUri);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }
#pragma warning disable CA1308 // Authenticator keys are conventionally displayed lowercase
        return result.ToString().ToLowerInvariant();
#pragma warning restore CA1308
    }
}
