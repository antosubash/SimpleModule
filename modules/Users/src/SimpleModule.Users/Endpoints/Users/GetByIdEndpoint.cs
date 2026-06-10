using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
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
                    ClaimsPrincipal principal,
                    IUserContracts userContracts,
                    IAuthorizer authorizer
                ) =>
                {
                    var user = await userContracts.GetUserByIdAsync(id);
                    if (user is null)
                    {
                        return TypedResults.NotFound();
                    }

                    // Self-or-admin; UserPolicy denies others as 404.
                    await authorizer.AuthorizeAsync(principal, PolicyActions.View, user);
                    return TypedResults.Ok(user);
                }
            )
            .WithTags(UsersConstants.ModuleName)
            .RequirePermission(UsersPermissions.View);
    }
}
