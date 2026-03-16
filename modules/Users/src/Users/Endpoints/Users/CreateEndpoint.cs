using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                UsersConstants.RoutePrefix,
                async (CreateUserRequest request, IUserContracts userContracts) =>
                {
                    var user = await userContracts.CreateUserAsync(request);
                    return TypedResults.Created($"{UsersConstants.RoutePrefix}/{user.Id}", user);
                }
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
