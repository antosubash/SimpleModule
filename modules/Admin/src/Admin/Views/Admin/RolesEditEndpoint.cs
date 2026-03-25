using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class RolesEditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/roles/{id}/edit",
                async (
                    string id,
                    RoleManager<ApplicationRole> roleManager,
                    UserManager<ApplicationUser> userManager,
                    IPermissionContracts permissionContracts,
                    PermissionRegistry permissionRegistry,
                    string? tab
                ) =>
                {
                    var role = await roleManager.FindByIdAsync(id);
                    if (role is null)
                        return TypedResults.NotFound();

                    var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
                    var users = usersInRole
                        .Select(u => new
                        {
                            id = u.Id,
                            displayName = u.DisplayName,
                            email = u.Email,
                        })
                        .ToList();

                    var rolePermissions = (
                        await permissionContracts.GetPermissionsForRoleAsync(RoleId.From(id))
                    ).ToList();

                    var permissionsByModule = permissionRegistry.ByModule.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToList()
                    );

                    return Inertia.Render(
                        "Admin/Admin/RolesEdit",
                        new
                        {
                            role = new
                            {
                                id = role.Id,
                                name = role.Name,
                                description = role.Description,
                                createdAt = role.CreatedAt.ToString("O"),
                            },
                            users,
                            rolePermissions,
                            permissionsByModule,
                            tab = tab ?? "details",
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
