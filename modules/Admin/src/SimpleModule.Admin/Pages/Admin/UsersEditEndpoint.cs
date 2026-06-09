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

                    // These three contracts are backed by DISTINCT stores —
                    // IRoleAdminContracts → UsersDbContext, IPermissionContracts →
                    // PermissionsDbContext, ISessionContracts → the OpenIddict token
                    // store / Keycloak — so unlike the AdminService case (#242) there
                    // is no shared-DbContext concurrency hazard and they run in
                    // parallel. (GetAdminUserByIdAsync above already completed, so no
                    // other in-flight UsersDbContext operation overlaps GetAllRolesAsync.)
                    var rolesTask = roleAdmin.GetAllRolesAsync();
                    var permsTask = permissionContracts.GetPermissionsForUserAsync(UserId.From(id));
                    var sessionsTask = sessionContracts.GetActiveSessionsForUserAsync(id);
                    await Task.WhenAll(rolesTask, permsTask, sessionsTask);

                    var allRoles = await rolesTask;
                    var userPermissions = (await permsTask).ToList();
                    var activeSessions = await sessionsTask;

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
