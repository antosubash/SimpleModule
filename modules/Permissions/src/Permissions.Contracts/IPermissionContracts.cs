using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions.Contracts;

public interface IPermissionContracts
{
    Task<IReadOnlySet<string>> GetPermissionsForUserAsync(UserId userId);
    Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(RoleId roleId);
    Task<IReadOnlySet<string>> GetAllPermissionsForUserAsync(
        UserId userId,
        IEnumerable<RoleId> roleIds
    );
    Task SetPermissionsForUserAsync(UserId userId, IEnumerable<string> permissions);
    Task SetPermissionsForRoleAsync(RoleId roleId, IEnumerable<string> permissions);
}
