using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Views.Admin;

public class UsersEndpoint : IViewEndpoint
{
    private const int PageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users",
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
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
