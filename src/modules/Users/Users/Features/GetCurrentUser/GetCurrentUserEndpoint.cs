using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Features.GetCurrentUser;

public static class GetCurrentUserEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group
            .MapGet(
                "/me",
                async Task<Results<Ok<UserDto>, NotFound>> (
                    HttpContext context,
                    IUserContracts userContracts
                ) =>
                {
                    var userId = context.User.GetClaim(OpenIddictConstants.Claims.Subject);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return TypedResults.NotFound();
                    }

                    var user = await userContracts.GetCurrentUserAsync(userId);
                    return user is not null ? TypedResults.Ok(user) : TypedResults.NotFound();
                }
            )
            .RequireAuthorization();
    }
}
