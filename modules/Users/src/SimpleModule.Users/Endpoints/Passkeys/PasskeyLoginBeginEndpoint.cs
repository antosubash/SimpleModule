using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class PasskeyLoginBeginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/passkeys/login/begin",
                async (SignInManager<ApplicationUser> signInManager) =>
                {
                    // MakePasskeyRequestOptionsAsync stores challenge in encrypted auth cookie.
                    // Pass null for user to allow any registered passkey (discoverable credentials).
                    var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(null);
                    return Results.Content(optionsJson, "application/json");
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithTags("Passkeys");
    }
}
