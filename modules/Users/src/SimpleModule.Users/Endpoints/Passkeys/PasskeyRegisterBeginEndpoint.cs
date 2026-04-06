using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class PasskeyRegisterBeginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/passkeys/register/begin",
                async (
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return Results.Unauthorized();

                    var userName = await userManager.GetUserNameAsync(user);
                    var displayName =
                        user.DisplayName.Length > 0
                            ? user.DisplayName
                            : (userName ?? user.Email ?? user.Id);

                    var userEntity = new PasskeyUserEntity
                    {
                        Id = await userManager.GetUserIdAsync(user),
                        Name = userName ?? user.Email ?? user.Id,
                        DisplayName = displayName,
                    };

                    var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(
                        userEntity
                    );
                    return Results.Content(optionsJson, "application/json");
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Passkeys");
    }
}
