using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public static class GetAllEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group
            .MapGet(
                "/",
                async (IUserContracts userContracts) =>
                {
                    var users = await userContracts.GetAllUsersAsync();
                    return TypedResults.Ok(users);
                }
            )
            .RequireAuthorization();
    }
}
