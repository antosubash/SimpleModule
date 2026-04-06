using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = UsersConstants.RoutePrefix + UsersConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                (IUserContracts userContracts) =>
                    CrudEndpoints.GetAll(userContracts.GetAllUsersAsync)
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
