using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Features.GetAllUsers;

public static class GetAllUsersEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/", async (IUserContracts userContracts) =>
        {
            var users = await userContracts.GetAllUsersAsync();
            return Results.Ok(users);
        });
    }
}
