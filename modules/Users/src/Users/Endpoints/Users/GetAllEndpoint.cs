using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                UsersConstants.RoutePrefix,
                async (IUserContracts userContracts) =>
                {
                    var users = await userContracts.GetAllUsersAsync();
                    return TypedResults.Ok(users);
                }
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
