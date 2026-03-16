using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
        group.MapPost(
            "/",
            async (HttpContext context, RoleManager<ApplicationRole> roleManager) =>
            {
                var form = await context.Request.ReadFormAsync();
                var name = form["name"].ToString().Trim();
                var description = form["description"].ToString().Trim();

                if (string.IsNullOrEmpty(name))
                    return Results.Redirect("/admin/roles/create");

                var role = new ApplicationRole
                {
                    Name = name,
                    Description = string.IsNullOrEmpty(description) ? null : description,
                };

                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                    return Results.Redirect("/admin/roles/create");

                return Results.Redirect("/admin/roles");
            }
        );

        // Update role
        group.MapPost(
            "/{id}",
            async (string id, HttpContext context, RoleManager<ApplicationRole> roleManager) =>
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role is null)
                    return Results.NotFound();

                var form = await context.Request.ReadFormAsync();
                role.Name = form["name"].ToString().Trim();
                var description = form["description"].ToString().Trim();
                role.Description = string.IsNullOrEmpty(description) ? null : description;

                await roleManager.UpdateAsync(role);

                return Results.Redirect($"/admin/roles/{id}/edit");
            }
        );

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

                return Results.Ok();
            }
        );
    }
}
