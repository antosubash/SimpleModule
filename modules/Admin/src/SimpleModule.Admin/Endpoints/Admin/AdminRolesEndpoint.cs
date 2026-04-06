using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Endpoints.Admin;

public class AdminRolesEndpoint : IEndpoint
{
    public const string Route = AdminConstants.Routes.RolesCreateApi;
    public const string Method = "POST";

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
                    IRoleAdminContracts roleAdmin,
                    IPermissionContracts permissionContracts
                ) =>
                {
                    var trimmedName = name.Trim();
                    if (string.IsNullOrEmpty(trimmedName))
                        return TypedResults.Redirect("/admin/roles/create");

                    var trimmedDescription = description?.Trim() is { Length: > 0 } d ? d : null;
                    var role = await roleAdmin.CreateRoleAsync(trimmedName, trimmedDescription);

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
                    IRoleAdminContracts roleAdmin
                ) =>
                {
                    var role = await roleAdmin.GetRoleByIdAsync(id);
                    if (role is null)
                        return TypedResults.NotFound();

                    var trimmedName = name.Trim();
                    var trimmedDescription = description?.Trim() is { Length: > 0 } d ? d : null;
                    await roleAdmin.UpdateRoleAsync(id, trimmedName, trimmedDescription);

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
                    IRoleAdminContracts roleAdmin,
                    IPermissionContracts permissionContracts
                ) =>
                {
                    var role = await roleAdmin.GetRoleByIdAsync(id);
                    if (role is null)
                        return TypedResults.NotFound();

                    var form = await context.Request.ReadFormAsync();
                    var newPermissions = form["permissions"]
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => p!)
                        .ToHashSet(StringComparer.Ordinal);

                    var roleId = RoleId.From(id);
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
                IRoleAdminContracts roleAdmin,
                IPermissionContracts permissionContracts
            ) =>
            {
                var role = await roleAdmin.GetRoleByIdAsync(id);
                if (role is null)
                    return TypedResults.NotFound();

                var hasUsers = await roleAdmin.HasUsersInRoleAsync(id);
                if (hasUsers)
                    return TypedResults.BadRequest(
                        new { error = "Cannot delete a role that has users assigned to it." }
                    );

                // Remove role permissions first
                var roleId = RoleId.From(id);
                await permissionContracts.SetPermissionsForRoleAsync(roleId, []);

                await roleAdmin.DeleteRoleAsync(id);

                return TypedResults.Redirect("/admin/roles");
            }
        );
    }
}
