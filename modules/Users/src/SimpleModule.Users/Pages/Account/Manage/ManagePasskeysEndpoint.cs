using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account.Manage;

public class ManagePasskeysEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Manage/Passkeys",
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    var passkeys = await userManager.GetPasskeysAsync(user);

                    var passkeysDto = passkeys.Select(p => new
                    {
                        credentialId = ToBase64Url(p.CredentialId),
                        name = p.Name,
                        createdAt = p.CreatedAt,
                    });

                    return Inertia.Render(
                        "Users/Account/Manage/Passkeys",
                        new { passkeys = passkeysDto }
                    );
                }
            )
            .RequireAuthorization();
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
