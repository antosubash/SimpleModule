using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Extensions;

namespace SimpleModule.Users.Endpoints.Users;

public class GetCurrentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                UsersConstants.RoutePrefix + UsersConstants.MeRoute,
                async Task<Results<Ok<UserDto>, NotFound>> (
                    ClaimsPrincipal principal,
                    IUserContracts userContracts
                ) =>
                {
                    var userIdStr = principal.GetUserId();
                    if (string.IsNullOrEmpty(userIdStr))
                    {
                        return TypedResults.NotFound();
                    }

                    var user = await userContracts.GetCurrentUserAsync(UserId.From(userIdStr));
                    return user is not null ? TypedResults.Ok(user) : TypedResults.NotFound();
                }
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
