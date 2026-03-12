using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public static class DeleteEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group
            .MapDelete(
                "/{id}",
                async (string id, IUserContracts userContracts) =>
                {
                    await userContracts.DeleteUserAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequireAuthorization();
    }
}
