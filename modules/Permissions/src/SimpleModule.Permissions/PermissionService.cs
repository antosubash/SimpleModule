using Microsoft.EntityFrameworkCore;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.Entities;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions;

public class PermissionService(PermissionsDbContext db) : IPermissionContracts
{
    public async Task<IReadOnlySet<string>> GetPermissionsForUserAsync(UserId userId)
    {
        var perms = await db
            .UserPermissions.Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

        return new HashSet<string>(perms);
    }

    public async Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(RoleId roleId)
    {
        var perms = await db
            .RolePermissions.Where(p => p.RoleId == roleId)
            .Select(p => p.Permission)
            .ToListAsync();

        return new HashSet<string>(perms);
    }

    public async Task<IReadOnlySet<string>> GetAllPermissionsForUserAsync(
        UserId userId,
        IEnumerable<RoleId> roleIds
    )
    {
        var roleIdList = roleIds.ToList();

        var rolePerms = await db
            .RolePermissions.Where(p => roleIdList.Contains(p.RoleId))
            .Select(p => p.Permission)
            .ToListAsync();

        var userPerms = await db
            .UserPermissions.Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

        var result = new HashSet<string>(rolePerms);
        foreach (var p in userPerms)
        {
            result.Add(p);
        }

        return result;
    }

    public async Task SetPermissionsForUserAsync(UserId userId, IEnumerable<string> permissions)
    {
        var existing = await db.UserPermissions.Where(p => p.UserId == userId).ToListAsync();

        db.UserPermissions.RemoveRange(existing);

        foreach (var permission in permissions)
        {
            db.UserPermissions.Add(new UserPermission { UserId = userId, Permission = permission });
        }

        await db.SaveChangesAsync();
    }

    public async Task SetPermissionsForRoleAsync(RoleId roleId, IEnumerable<string> permissions)
    {
        var existing = await db.RolePermissions.Where(p => p.RoleId == roleId).ToListAsync();

        db.RolePermissions.RemoveRange(existing);

        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, Permission = permission });
        }

        await db.SaveChangesAsync();
    }
}
