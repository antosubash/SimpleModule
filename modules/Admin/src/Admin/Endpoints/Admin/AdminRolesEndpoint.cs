using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Services;
using SimpleModule.Core;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Endpoints.Admin;

public class AdminRolesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/roles")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();

        // POST / — Create role with permissions
        group
            .MapPost(
                "/",
                async (
                    [FromForm] string name,
                    [FromForm] string? description,
                    HttpContext context,
                    RoleManager<ApplicationRole> roleManager,
                    IPermissionContracts permissionContracts,
                    AuditService audit
                ) =>
                {
                    var trimmedName = name.Trim();
                    if (string.IsNullOrEmpty(trimmedName))
                        return TypedResults.Redirect("/admin/roles/create");

                    var role = new ApplicationRole
                    {
                        Name = trimmedName,
                        Description = description?.Trim() is { Length: > 0 } d ? d : null,
                    };

                    var result = await roleManager.CreateAsync(role);
                    if (!result.Succeeded)
                        return TypedResults.Redirect("/admin/roles/create");

                    var form = await context.Request.ReadFormAsync();
                    var filteredPermissions = form["permissions"]
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => p!)
                        .ToList();

                    if (filteredPermissions.Count > 0)
                    {
                        await permissionContracts.SetPermissionsForRoleAsync(
                            RoleId.From(role.Id),
                            filteredPermissions
                        );
                    }

                    var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                    await audit.LogAsync(
                        role.Id,
                        adminUserId,
                        "RoleCreated",
                        $"Role '{trimmedName}' created"
                    );

                    return TypedResults.Redirect($"/admin/roles/{role.Id}/edit");
                }
            )
            .DisableAntiforgery();

        // POST /{id} — Update role details
        group
            .MapPost(
                "/{id}",
                async Task<IResult> (
                    string id,
                    [FromForm] string name,
                    [FromForm] string? description,
                    HttpContext context,
                    RoleManager<ApplicationRole> roleManager,
                    AuditService audit
                ) =>
                {
                    var role = await roleManager.FindByIdAsync(id);
                    if (role is null)
                        return TypedResults.NotFound();

                    role.Name = name.Trim();
                    role.Description = description?.Trim() is { Length: > 0 } d ? d : null;
                    await roleManager.UpdateAsync(role);

                    var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                    await audit.LogAsync(
                        role.Id,
                        adminUserId,
                        "RoleUpdated",
                        $"Role '{role.Name}' updated"
                    );

                    return TypedResults.Redirect($"/admin/roles/{id}/edit?tab=details");
                }
            )
            .DisableAntiforgery();

        // POST /{id}/permissions — Set role permissions
        group
            .MapPost(
                "/{id}/permissions",
                async Task<IResult> (
                    string id,
                    HttpContext context,
                    RoleManager<ApplicationRole> roleManager,
                    IPermissionContracts permissionContracts,
                    AuditService audit
                ) =>
                {
                    var role = await roleManager.FindByIdAsync(id);
                    if (role is null)
                        return TypedResults.NotFound();

                    var form = await context.Request.ReadFormAsync();
                    var newPermissions = form["permissions"]
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => p!)
                        .ToHashSet(StringComparer.Ordinal);

                    var roleId = RoleId.From(id);
                    var currentPermissions = await permissionContracts.GetPermissionsForRoleAsync(
                        roleId
                    );

                    var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    // Audit removed permissions
                    foreach (var perm in currentPermissions.Where(p => !newPermissions.Contains(p)))
                    {
                        await audit.LogAsync(
                            role.Id,
                            adminUserId,
                            "RolePermissionRemoved",
                            $"Permission '{perm}' removed from role '{role.Name}'"
                        );
                    }

                    // Audit added permissions
                    foreach (var perm in newPermissions.Where(p => !currentPermissions.Contains(p)))
                    {
                        await audit.LogAsync(
                            role.Id,
                            adminUserId,
                            "RolePermissionAdded",
                            $"Permission '{perm}' added to role '{role.Name}'"
                        );
                    }

                    await permissionContracts.SetPermissionsForRoleAsync(roleId, newPermissions);

                    return TypedResults.Redirect($"/admin/roles/{id}/edit?tab=permissions");
                }
            )
            .DisableAntiforgery();

        // DELETE /{id} — Delete role
        group.MapDelete(
            "/{id}",
            async Task<IResult> (
                string id,
                HttpContext context,
                RoleManager<ApplicationRole> roleManager,
                UserManager<ApplicationUser> userManager,
                IPermissionContracts permissionContracts,
                AuditService audit
            ) =>
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role is null)
                    return TypedResults.NotFound();

                var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
                if (usersInRole.Count > 0)
                    return TypedResults.BadRequest(
                        new { error = "Cannot delete a role that has users assigned to it." }
                    );

                // Remove role permissions first
                await permissionContracts.SetPermissionsForRoleAsync(RoleId.From(id), []);

                var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                await audit.LogAsync(
                    role.Id,
                    adminUserId,
                    "RoleDeleted",
                    $"Role '{role.Name}' deleted"
                );

                await roleManager.DeleteAsync(role);

                return TypedResults.Redirect("/admin/roles");
            }
        );
    }
}
