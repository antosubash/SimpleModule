using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class PasskeyLoginCompleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/passkeys/login/complete",
                async (
                    HttpRequest request,
                    SignInManager<ApplicationUser> signInManager,
                    [FromQuery] string? returnUrl = null
                ) =>
                {
                    string credentialJson;
                    using (var reader = new StreamReader(request.Body, leaveOpen: true))
                    {
                        credentialJson = await reader.ReadToEndAsync();
                    }

                    if (string.IsNullOrWhiteSpace(credentialJson))
                        return Results.BadRequest("Credential JSON is required.");

                    // PerformPasskeyAssertionAsync validates the WebAuthn assertion.
                    // It throws InvalidOperationException if no assertion challenge cookie exists
                    // (i.e. login/begin was not called first) or PasskeyException on crypto failures.
                    Microsoft.AspNetCore.Identity.SignInResult result;
                    try
                    {
                        result = await signInManager.PasskeySignInAsync(credentialJson);
                    }
                    catch (InvalidOperationException)
                    {
                        return Results.Unauthorized();
                    }
                    catch (PasskeyException)
                    {
                        return Results.Unauthorized();
                    }

                    if (result.Succeeded)
                    {
                        var redirectUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
                        return Results.Ok(new { redirectUrl });
                    }

                    if (result.IsLockedOut)
                        return Results.Problem("Account is locked out.", statusCode: 423);

                    return Results.Unauthorized();
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithTags("Passkeys");
    }
}
