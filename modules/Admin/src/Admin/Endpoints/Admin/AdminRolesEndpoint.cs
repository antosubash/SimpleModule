using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Admin.Services;
using SimpleModule.Core;
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
        group.MapPost(
                "/",
                async (
                    [FromForm] string name,
                    [FromForm] string? description,
                    HttpContext context,
                    RoleManager<ApplicationRole> roleManager,
                    UsersDbContext usersDb,
                    AuditService audit
                ) =>
                {
                    var trimmedName = name.Trim();
                    if (string.IsNullOrEmpty(trimmedName))
                        return Results.Redirect("/admin/roles/create");

                    var role = new ApplicationRole
                    {
                        Name = trimmedName,
                        Description = description?.Trim() is { Length: > 0 } d ? d : null,
                    };

                    var result = await roleManager.CreateAsync(role);
                    if (!result.Succeeded)
                        return Results.Redirect("/admin/roles/create");

                    var form = await context.Request.ReadFormAsync();
                    var permissions = form["permissions"].ToArray();
                    foreach (var permission in permissions)
                    {
                        if (!string.IsNullOrWhiteSpace(permission))
                        {
                            usersDb.RolePermissions.Add(new RolePermission
                            {
                                RoleId = role.Id,
                                Permission = permission,
                            });
                        }
                    }

                    await usersDb.SaveChangesAsync();

                    var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                    await audit.LogAsync(role.Id, adminUserId, "RoleCreated", $"Role '{trimmedName}' created");

                    return Results.Redirect($"/admin/roles/{role.Id}/edit");
                }
            )
            .DisableAntiforgery();

        // POST /{id} — Update role details
        group.MapPost(
                "/{id}",
                async (
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
                        return Results.NotFound();

                    role.Name = name.Trim();
                    role.Description = description?.Trim() is { Length: > 0 } d ? d : null;
                    await roleManager.UpdateAsync(role);

                    var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                    await audit.LogAsync(role.Id, adminUserId, "RoleUpdated", $"Role '{role.Name}' updated");

                    return Results.Redirect($"/admin/roles/{id}/edit?tab=details");
                }
            )
            .DisableAntiforgery();

        // POST /{id}/permissions — Set role permissions
        group.MapPost(
                "/{id}/permissions",
                async (
                    string id,
                    HttpContext context,
                    RoleManager<ApplicationRole> roleManager,
                    UsersDbContext usersDb,
                    AuditService audit
                ) =>
                {
                    var role = await roleManager.FindByIdAsync(id);
                    if (role is null)
                        return Results.NotFound();

                    var form = await context.Request.ReadFormAsync();
                    var newPermissions = form["permissions"]
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => p!)
                        .ToHashSet(StringComparer.Ordinal);

                    var currentPermissions = await usersDb.RolePermissions
                        .Where(rp => rp.RoleId == id)
                        .ToListAsync();

                    var currentSet = currentPermissions
                        .Select(rp => rp.Permission)
                        .ToHashSet(StringComparer.Ordinal);

                    var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    // Remove permissions no longer in set
                    var toRemove = currentPermissions
                        .Where(rp => !newPermissions.Contains(rp.Permission))
                        .ToList();
                    foreach (var rp in toRemove)
                    {
                        usersDb.RolePermissions.Remove(rp);
                        await audit.LogAsync(role.Id, adminUserId, "RolePermissionRemoved", $"Permission '{rp.Permission}' removed from role '{role.Name}'");
                    }

                    // Add new permissions
                    var toAdd = newPermissions.Where(p => !currentSet.Contains(p)).ToList();
                    foreach (var permission in toAdd)
                    {
                        usersDb.RolePermissions.Add(new RolePermission
                        {
                            RoleId = id,
                            Permission = permission,
                        });
                        await audit.LogAsync(role.Id, adminUserId, "RolePermissionAdded", $"Permission '{permission}' added to role '{role.Name}'");
                    }

                    await usersDb.SaveChangesAsync();

                    return Results.Redirect($"/admin/roles/{id}/edit?tab=permissions");
                }
            )
            .DisableAntiforgery();

        // DELETE /{id} — Delete role
        group.MapDelete(
                "/{id}",
                async (
                    string id,
                    HttpContext context,
                    RoleManager<ApplicationRole> roleManager,
                    UserManager<ApplicationUser> userManager,
                    UsersDbContext usersDb,
                    AuditService audit
                ) =>
                {
                    var role = await roleManager.FindByIdAsync(id);
                    if (role is null)
                        return Results.NotFound();

                    var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
                    if (usersInRole.Count > 0)
                        return Results.BadRequest(new { error = "Cannot delete a role that has users assigned to it." });

                    // Remove role permissions first
                    var permissions = await usersDb.RolePermissions
                        .Where(rp => rp.RoleId == id)
                        .ToListAsync();
                    usersDb.RolePermissions.RemoveRange(permissions);
                    await usersDb.SaveChangesAsync();

                    var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                    await audit.LogAsync(role.Id, adminUserId, "RoleDeleted", $"Role '{role.Name}' deleted");

                    await roleManager.DeleteAsync(role);

                    return Results.Redirect("/admin/roles");
                }
            );
    }
}
