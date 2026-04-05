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
        var usersTask = userAdmin.GetUsersPagedAsync(null, 1, 1);
        var activeTask = userAdmin.GetUsersPagedAsync(null, 1, 1, filterStatus: "active");
        var rolesTask = roleAdmin.GetAllRolesAsync();
        await Task.WhenAll(usersTask, activeTask, rolesTask).ConfigureAwait(false);

        var usersPage = await usersTask.ConfigureAwait(false);
        var activePage = await activeTask.ConfigureAwait(false);
        var roles = await rolesTask.ConfigureAwait(false);

        return new AdminOverviewDto
        {
            TotalUsers = usersPage.TotalCount,
            ActiveUsers = activePage.TotalCount,
            TotalRoles = roles.Count,
        };
    }
}
