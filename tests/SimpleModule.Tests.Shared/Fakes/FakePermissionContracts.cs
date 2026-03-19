using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public class FakePermissionContracts : IPermissionContracts
{
    private readonly Dictionary<string, HashSet<string>> _userPermissions = new();
    private readonly Dictionary<string, HashSet<string>> _rolePermissions = new();

    public Task<IReadOnlySet<string>> GetPermissionsForUserAsync(UserId userId)
    {
        return Task.FromResult<IReadOnlySet<string>>(
            _userPermissions.TryGetValue(userId.Value, out var perms)
                ? perms
                : new HashSet<string>()
        );
    }

    public Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(RoleId roleId)
    {
        return Task.FromResult<IReadOnlySet<string>>(
            _rolePermissions.TryGetValue(roleId.Value, out var perms)
                ? perms
                : new HashSet<string>()
        );
    }

    public async Task<IReadOnlySet<string>> GetAllPermissionsForUserAsync(
        UserId userId,
        IEnumerable<RoleId> roleIds
    )
    {
        var result = new HashSet<string>();

        var userPerms = await GetPermissionsForUserAsync(userId);
        result.UnionWith(userPerms);

        foreach (var roleId in roleIds)
        {
            var rolePerms = await GetPermissionsForRoleAsync(roleId);
            result.UnionWith(rolePerms);
        }

        return result;
    }

    public Task SetPermissionsForUserAsync(UserId userId, IEnumerable<string> permissions)
    {
        _userPermissions[userId.Value] = new HashSet<string>(permissions);
        return Task.CompletedTask;
    }

    public Task SetPermissionsForRoleAsync(RoleId roleId, IEnumerable<string> permissions)
    {
        _rolePermissions[roleId.Value] = new HashSet<string>(permissions);
        return Task.CompletedTask;
    }
}
