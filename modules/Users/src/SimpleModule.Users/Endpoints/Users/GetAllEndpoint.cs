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
                (IUserContracts userContracts, int? skip, int? take) =>
                    CrudEndpoints.GetAll(() =>
                        userContracts.GetAllUsersAsync(
                            Math.Max(0, skip ?? 0),
                            Math.Clamp(take ?? 30, 1, 200)
                        )
                    )
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
