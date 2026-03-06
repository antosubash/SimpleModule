using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Features.GetUserById;

public static class GetUserByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet(
            "/{id}",
            async Task<Results<Ok<User>, NotFound>> (int id, IUserContracts userContracts) =>
            {
                var user = await userContracts.GetUserByIdAsync(id);
                return user is not null ? TypedResults.Ok(user) : TypedResults.NotFound();
            }
        );
    }
}
