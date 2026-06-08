using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Pages.Admin;

public class UsersEndpoint : IViewEndpoint
{
    public const string Route = AdminConstants.Routes.Users;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
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

                    // Await sequentially, not via Task.WhenAll: IUserAdminContracts and
                    // IRoleAdminContracts are both backed by the same scoped UsersDbContext,
                    // and EF Core forbids concurrent operations on one DbContext instance.
                    // Running them in parallel intermittently threw "A second operation was
                    // started on this context instance..." → HTTP 500 (#242). The roles list
                    // is tiny, so the extra round-trip is negligible.
                    var result = await userAdmin.GetUsersPagedAsync(
                        search,
                        page,
                        pageSize,
                        filterStatus,
                        filterRole
                    );
                    var allRoles = await roleAdmin.GetAllRolesAsync();
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
            .RequireAuthorization(policy => policy.RequireRole(WellKnownRoles.Admin));
    }
}
