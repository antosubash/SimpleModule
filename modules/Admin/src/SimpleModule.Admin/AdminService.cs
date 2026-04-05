using SimpleModule.Admin.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin;

public sealed class AdminService(IUserAdminContracts userAdmin, IRoleAdminContracts roleAdmin)
    : IAdminContracts
{
    public async Task<AdminOverviewDto> GetAdminOverviewAsync(
        CancellationToken cancellationToken = default
    )
    {
        var usersPage = await userAdmin.GetUsersPagedAsync(null, 1, 1).ConfigureAwait(false);
        var activePage = await userAdmin
            .GetUsersPagedAsync(null, 1, 1, filterStatus: "active")
            .ConfigureAwait(false);
        var roles = await roleAdmin.GetAllRolesAsync().ConfigureAwait(false);

        return new AdminOverviewDto
        {
            TotalUsers = usersPage.TotalCount,
            ActiveUsers = activePage.TotalCount,
            TotalRoles = roles.Count,
        };
    }
}
