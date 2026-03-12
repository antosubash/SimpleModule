using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public static class UpdateEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group
            .MapPut(
                "/{id}",
                async Task<Results<Ok<UserDto>, NotFound>> (
                    string id,
                    UpdateUserRequest request,
                    IUserContracts userContracts
                ) =>
                {
                    var user = await userContracts.UpdateUserAsync(id, request);
                    return TypedResults.Ok(user);
                }
            )
            .RequireAuthorization();
    }
}
