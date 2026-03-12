using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Extensions;

namespace SimpleModule.Users.Endpoints.Users;

public static class GetCurrentEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group
            .MapGet(
                UsersConstants.MeRoute,
                async Task<Results<Ok<UserDto>, NotFound>> (
                    HttpContext context,
                    IUserContracts userContracts
                ) =>
                {
                    var userId = context.User.GetUserId();
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
