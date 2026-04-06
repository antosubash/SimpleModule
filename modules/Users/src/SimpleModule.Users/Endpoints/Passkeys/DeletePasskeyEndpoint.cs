using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class DeletePasskeyEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/api/passkeys/{credentialId}",
                async (
                    string credentialId,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return Results.Unauthorized();

                    byte[] credentialIdBytes;
                    try
                    {
                        // Decode base64url (URL-safe base64 without padding)
                        var base64 = credentialId.Replace('-', '+').Replace('_', '/');
                        var padding = (4 - (base64.Length % 4)) % 4;
                        base64 = base64.PadRight(base64.Length + padding, '=');
                        credentialIdBytes = Convert.FromBase64String(base64);
                    }
                    catch (FormatException)
                    {
                        return Results.BadRequest("Invalid credential ID format.");
                    }

                    // Verify the passkey belongs to this user before deleting
                    var passkeys = await userManager.GetPasskeysAsync(user);
                    var exists = passkeys.Any(p => p.CredentialId.SequenceEqual(credentialIdBytes));
                    if (!exists)
                        return Results.NotFound();

                    await userManager.RemovePasskeyAsync(user, credentialIdBytes);
                    return Results.NoContent();
                }
            )
            .RequireAuthorization()
            .WithTags("Passkeys");
    }
}
