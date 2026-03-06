using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Features.GetUserById;

public static class GetUserByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/{id}", async (int id, IUserContracts userContracts) =>
        {
            var user = await userContracts.GetUserByIdAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        });
    }
}
