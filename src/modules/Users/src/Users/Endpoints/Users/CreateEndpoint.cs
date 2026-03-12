using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public static class CreateEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group
            .MapPost(
                "/",
                async (CreateUserRequest request, IUserContracts userContracts) =>
                {
                    var user = await userContracts.CreateUserAsync(request);
                    return TypedResults.Created($"{UsersConstants.RoutePrefix}/{user.Id}", user);
                }
            )
            .RequireAuthorization();
    }
}
