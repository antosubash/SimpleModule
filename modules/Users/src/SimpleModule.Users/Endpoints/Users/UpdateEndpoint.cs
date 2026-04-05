using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class UpdateEndpoint : IEndpoint
{
    public const string Route = UsersConstants.RoutePrefix + UsersConstants.Routes.Update;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
                Route,
                async Task<Results<Ok<UserDto>, NotFound>> (
                    UserId id,
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
