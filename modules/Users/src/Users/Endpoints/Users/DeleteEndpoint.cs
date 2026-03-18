using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Ids;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                UsersConstants.RoutePrefix + "/{id}",
                async (UserId id, IUserContracts userContracts) =>
                {
                    await userContracts.DeleteUserAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
