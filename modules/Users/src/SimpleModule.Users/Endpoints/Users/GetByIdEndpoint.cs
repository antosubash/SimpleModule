using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = UsersConstants.RoutePrefix + UsersConstants.Routes.GetById;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async Task<Results<Ok<UserDto>, NotFound>> (
                    UserId id,
                    IUserContracts userContracts
                ) =>
                {
                    var user = await userContracts.GetUserByIdAsync(id);
                    return user is not null ? TypedResults.Ok(user) : TypedResults.NotFound();
                }
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
