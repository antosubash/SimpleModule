using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class PasskeyRegisterCompleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/passkeys/register/complete",
                async (
                    HttpRequest request,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return Results.Unauthorized();

                    string credentialJson;
                    using (var reader = new StreamReader(request.Body, leaveOpen: true))
                    {
                        credentialJson = await reader.ReadToEndAsync();
                    }
                    if (string.IsNullOrWhiteSpace(credentialJson))
                        return Results.BadRequest("Credential JSON is required.");

                    // PerformPasskeyAttestationAsync validates the WebAuthn attestation.
                    // It throws InvalidOperationException if no attestation challenge cookie exists
                    // (i.e. register/begin was not called first) or PasskeyException on crypto failures.
                    PasskeyAttestationResult result;
                    try
                    {
                        result = await signInManager.PerformPasskeyAttestationAsync(credentialJson);
                    }
                    catch (InvalidOperationException)
                    {
                        return Results.BadRequest("No passkey registration in progress.");
                    }
                    catch (PasskeyException)
                    {
                        return Results.BadRequest("Passkey registration failed.");
                    }

                    if (!result.Succeeded)
                        return Results.BadRequest("Passkey registration failed.");

                    await userManager.AddOrUpdatePasskeyAsync(user, result.Passkey);
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Passkeys");
    }
}
