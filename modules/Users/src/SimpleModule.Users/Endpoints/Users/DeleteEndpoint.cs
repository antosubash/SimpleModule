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

public class DeleteEndpoint : IEndpoint
{
    public const string Route = UsersConstants.RoutePrefix + UsersConstants.Routes.Delete;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                Route,
                async Task<Results<NoContent, NotFound>> (
                    UserId id,
                    ClaimsPrincipal principal,
                    IUserContracts userContracts,
                    IAuthorizer authorizer
                ) =>
                {
                    // Load → authorize → act: deletion is admin-only (UserPolicy).
                    var existing = await userContracts.GetUserByIdAsync(id);
                    if (existing is null)
                    {
                        return TypedResults.NotFound();
                    }

                    await authorizer.AuthorizeAsync(principal, PolicyActions.Delete, existing);

                    await userContracts.DeleteUserAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .WithTags(UsersConstants.ModuleName)
            .RequirePermission(UsersPermissions.Delete);
    }
}
