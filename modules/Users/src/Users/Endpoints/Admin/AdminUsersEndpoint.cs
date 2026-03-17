using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Endpoints.Admin;

public class AdminUsersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/users")
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Update user
        group
            .MapPost(
                "/{id}",
                async (
                    string id,
                    [FromForm] string displayName,
                    [FromForm] string email,
                    [FromForm] string? emailConfirmed,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    var user = await userManager.FindByIdAsync(id);
                    if (user is null)
                        return Results.NotFound();

                    user.DisplayName = displayName;
                    user.Email = email;
                    user.EmailConfirmed = emailConfirmed is not null;

                    await userManager.UpdateAsync(user);

                    return Results.Redirect($"/admin/users/{id}/edit");
                }
            )
            .DisableAntiforgery();

        // Set roles
        group
            .MapPost(
                "/{id}/roles",
                async (string id, HttpContext context, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.FindByIdAsync(id);
                    if (user is null)
                        return Results.NotFound();

                    var form = await context.Request.ReadFormAsync();
                    var newRoles = form["roles"]
                        .ToArray()
                        .Where(r => !string.IsNullOrEmpty(r))
                        .ToList();
                    var currentRoles = await userManager.GetRolesAsync(user);

                    await userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (newRoles.Count > 0)
                        await userManager.AddToRolesAsync(user, newRoles!);

                    return Results.Redirect($"/admin/users/{id}/edit");
                }
            )
            .DisableAntiforgery();

        // Lock account
        group.MapPost(
            "/{id}/lock",
            async (string id, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                    return Results.NotFound();

                await userManager.SetLockoutEnabledAsync(user, true);
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

                return Results.Redirect($"/admin/users/{id}/edit");
            }
        );

        // Unlock account
        group.MapPost(
            "/{id}/unlock",
            async (string id, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                    return Results.NotFound();

                await userManager.SetLockoutEndDateAsync(user, null);
                await userManager.ResetAccessFailedCountAsync(user);

                return Results.Redirect($"/admin/users/{id}/edit");
            }
        );
    }
}
