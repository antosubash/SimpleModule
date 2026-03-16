using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
                UsersConstants.RoutePrefix + "/{id}",
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
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
