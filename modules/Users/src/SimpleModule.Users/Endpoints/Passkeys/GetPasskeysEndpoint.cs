using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class GetPasskeysEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/passkeys",
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return Results.Unauthorized();

                    var passkeys = await userManager.GetPasskeysAsync(user);

                    var result = passkeys.Select(p => new
                    {
                        credentialId = PasskeyHelpers.ToBase64Url(p.CredentialId),
                        name = p.Name,
                        createdAt = p.CreatedAt,
                    });

                    return Results.Ok(result);
                }
            )
            .RequireAuthorization()
            .WithTags("Passkeys");
    }
}
