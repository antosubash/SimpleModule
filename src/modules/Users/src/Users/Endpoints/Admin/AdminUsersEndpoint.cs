using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Endpoints.Admin;

public static class AdminUsersEndpoint
{
    private const int PageSize = 20;

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/admin/users")
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // List users
        group.MapGet(
            "/",
            async (
                UserManager<ApplicationUser> userManager,
                RoleManager<ApplicationRole> roleManager,
                string? search,
                int page = 1
            ) =>
            {
                var query = userManager.Users.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var pattern = $"%{search.Trim()}%";
                    query = query.Where(u =>
                        (u.Email != null && EF.Functions.Like(u.Email, pattern))
                        || EF.Functions.Like(u.DisplayName, pattern)
                        || (u.UserName != null && EF.Functions.Like(u.UserName, pattern))
                    );
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                page = Math.Clamp(page, 1, Math.Max(1, totalPages));

                var users = await query
                    .OrderBy(u => u.DisplayName)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                var userList = new List<object>();
                foreach (var user in users)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    userList.Add(
                        new
                        {
                            id = user.Id,
                            displayName = user.DisplayName,
                            email = user.Email,
                            emailConfirmed = user.EmailConfirmed,
                            roles = roles.ToList(),
                            isLockedOut = user.LockoutEnd.HasValue
                                && user.LockoutEnd > DateTimeOffset.UtcNow,
                            createdAt = user.CreatedAt.ToString("O"),
                        }
                    );
                }

                return Inertia.Render(
                    "Users/Admin/Users",
                    new
                    {
                        users = userList,
                        search = search ?? "",
                        page,
                        totalPages,
                        totalCount,
                    }
                );
            }
        );

        // Edit user page
        group.MapGet(
            "/{id}/edit",
            async (
                string id,
                UserManager<ApplicationUser> userManager,
                RoleManager<ApplicationRole> roleManager
            ) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                    return Results.NotFound();

                var userRoles = await userManager.GetRolesAsync(user);
                var allRoles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

                return Inertia.Render(
                    "Users/Admin/UsersEdit",
                    new
                    {
                        user = new
                        {
                            id = user.Id,
                            displayName = user.DisplayName,
                            email = user.Email,
                            emailConfirmed = user.EmailConfirmed,
                            isLockedOut = user.LockoutEnd.HasValue
                                && user.LockoutEnd > DateTimeOffset.UtcNow,
                            createdAt = user.CreatedAt.ToString("O"),
                            lastLoginAt = user.LastLoginAt?.ToString("O"),
                        },
                        userRoles = userRoles.ToList(),
                        allRoles = allRoles
                            .Select(r => new
                            {
                                id = r.Id,
                                name = r.Name,
                                description = r.Description,
                            })
                            .ToList(),
                    }
                );
            }
        );

        // Update user
        group.MapPost(
            "/{id}",
            async (string id, HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                    return Results.NotFound();

                var form = await context.Request.ReadFormAsync();
                user.DisplayName = form["displayName"].ToString();
                user.Email = form["email"].ToString();
                user.EmailConfirmed = form.ContainsKey("emailConfirmed");

                await userManager.UpdateAsync(user);

                return Results.Redirect($"/admin/users/{id}/edit");
            }
        );

        // Set roles
        group.MapPost(
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
        );

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
