using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.OpenIddict.Contracts;
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
                    IOpenIddictSessionContracts sessionContracts,
                    PermissionRegistry permissionRegistry,
                    string? tab
                ) =>
                {
                    var user = await userAdmin.GetAdminUserByIdAsync(UserId.From(id));
                    if (user is null)
                        return TypedResults.NotFound();

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
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
