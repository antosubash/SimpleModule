using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Users;

public class CreateEndpoint : IEndpoint
{
    public const string Route = UsersConstants.RoutePrefix + UsersConstants.Routes.Create;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                Route,
                (CreateUserRequest request, IUserContracts userContracts) =>
                    CrudEndpoints.Create(
                        () => userContracts.CreateUserAsync(request),
                        u => $"{UsersConstants.RoutePrefix}/{u.Id}"
                    )
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
