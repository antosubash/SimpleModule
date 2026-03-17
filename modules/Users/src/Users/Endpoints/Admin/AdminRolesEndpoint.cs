using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Endpoints.Admin;

public class AdminRolesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/roles")
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Create role
        group
            .MapPost(
                "/",
                async (
                    [FromForm] string name,
                    [FromForm] string? description,
                    RoleManager<ApplicationRole> roleManager
                ) =>
                {
                    var trimmedName = name.Trim();
                    var trimmedDescription = description?.Trim();

                    if (string.IsNullOrEmpty(trimmedName))
                        return Results.Redirect("/admin/roles/create");

                    var role = new ApplicationRole
                    {
                        Name = trimmedName,
                        Description = string.IsNullOrEmpty(trimmedDescription)
                            ? null
                            : trimmedDescription,
                    };

                    var result = await roleManager.CreateAsync(role);
                    if (!result.Succeeded)
                        return Results.Redirect("/admin/roles/create");

                    return Results.Redirect("/admin/roles");
                }
            )
            .DisableAntiforgery();

        // Update role
        group
            .MapPost(
                "/{id}",
                async (
                    string id,
                    [FromForm] string name,
                    [FromForm] string? description,
                    RoleManager<ApplicationRole> roleManager
                ) =>
                {
                    var role = await roleManager.FindByIdAsync(id);
                    if (role is null)
                        return Results.NotFound();

                    role.Name = name.Trim();
                    var trimmedDescription = description?.Trim();
                    role.Description = string.IsNullOrEmpty(trimmedDescription)
                        ? null
                        : trimmedDescription;

                    await roleManager.UpdateAsync(role);

                    return Results.Redirect($"/admin/roles/{id}/edit");
                }
            )
            .DisableAntiforgery();

        // Delete role
        group.MapDelete(
            "/{id}",
            async (
                string id,
                RoleManager<ApplicationRole> roleManager,
                UserManager<ApplicationUser> userManager
            ) =>
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role is null)
                    return Results.NotFound();

                // Don't delete if users are assigned
                var usersInRole = role.Name is not null
                    ? await userManager.GetUsersInRoleAsync(role.Name)
                    : [];
                if (usersInRole.Count > 0)
                    return Results.BadRequest(
                        new { error = "Cannot delete role with assigned users" }
                    );

                await roleManager.DeleteAsync(role);

                return Results.Redirect("/admin/roles");
            }
        );
    }
}
