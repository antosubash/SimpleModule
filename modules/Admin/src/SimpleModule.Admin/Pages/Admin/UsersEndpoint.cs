using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Pages.Admin;

public class UsersEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/users",
                async (
                    IUserAdminContracts userAdmin,
                    IRoleAdminContracts roleAdmin,
                    IOptions<AdminModuleOptions> options,
                    string? search,
                    string? filterStatus,
                    string? filterRole,
                    int page = 1
                ) =>
                {
                    var pageSize = options.Value.UsersPageSize;
                    var usersTask = userAdmin.GetUsersPagedAsync(
                        search,
                        page,
                        pageSize,
                        filterStatus,
                        filterRole
                    );
                    var rolesTask = roleAdmin.GetAllRolesAsync();
                    await Task.WhenAll(usersTask, rolesTask);

                    var result = await usersTask;
                    var allRoles = await rolesTask;
                    var totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);

                    return Inertia.Render(
                        "Admin/Admin/Users",
                        new
                        {
                            users = result.Items,
                            search = search ?? "",
                            page = result.Page,
                            totalPages,
                            totalCount = result.TotalCount,
                            allRoles = allRoles.Select(r => r.Name).ToList(),
                            filterStatus = filterStatus ?? "",
                            filterRole = filterRole ?? "",
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
