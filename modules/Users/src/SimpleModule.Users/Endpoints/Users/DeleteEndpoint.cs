using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                UsersConstants.RoutePrefix + "/{id}",
                (UserId id, IUserContracts userContracts) =>
                    CrudEndpoints.Delete(() => userContracts.DeleteUserAsync(id))
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
