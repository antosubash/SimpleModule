using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Identity.Contracts;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Pages.Admin;

public class UsersEditEndpoint : IViewEndpoint
{
    public const string Route = AdminConstants.Routes.UsersEdit;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (
                    string id,
                    HttpContext context,
                    IUserAdminContracts userAdmin,
                    IRoleAdminContracts roleAdmin,
                    IPermissionContracts permissionContracts,
                    ISessionContracts sessionContracts,
                    PermissionRegistry permissionRegistry,
                    string? tab
                ) =>
                {
                    var user = await userAdmin.GetAdminUserByIdAsync(UserId.From(id));
                    if (user is null)
                        return TypedResults.NotFound();

                    // Await sequentially, not via Task.WhenAll: these contracts can
                    // resolve services backed by the same scoped DbContext, and EF Core
                    // forbids concurrent operations on one context instance. Parallel
                    // awaits intermittently surfaced as HTTP 500 (same class as #242).
                    var allRoles = await roleAdmin.GetAllRolesAsync();
                    var userPermissions = (
                        await permissionContracts.GetPermissionsForUserAsync(UserId.From(id))
                    ).ToList();
                    var activeSessions = await sessionContracts.GetActiveSessionsForUserAsync(id);

                    var permissionsByModule = permissionRegistry.ByModule.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToList()
                    );

                    var currentUserId =
                        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    return Inertia.Render(
                        "Admin/Admin/UsersEdit",
                        new
                        {
                            user,
                            userPermissions,
                            allRoles,
                            permissionsByModule,
                            activeSessions,
                            tab = tab ?? "details",
                            currentUserId,
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole(WellKnownRoles.Admin));
    }
}
