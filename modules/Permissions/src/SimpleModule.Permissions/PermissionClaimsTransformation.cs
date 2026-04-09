using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using SimpleModule.Core.Caching;
using SimpleModule.Core.Extensions;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions;

public sealed class PermissionClaimsTransformation(
    IPermissionContracts permissionContracts,
    IUserContracts userContracts,
    ICacheStore cache
) : IClaimsTransformation
{
    private static readonly CacheEntryOptions CacheOptions = CacheEntryOptions.Expires(
        TimeSpan.FromMinutes(5)
    );

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return principal;
        }

        // Skip if permission claims are already present (e.g., from an OAuth token)
        if (principal.HasClaim(c => c.Type == "permission"))
        {
            return principal;
        }

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var rolesKey = string.Join(',', roles.Order());
        var cacheKey = $"permissions:{userId}:{rolesKey}";

        var permissions =
            await cache.GetOrCreateAsync<IReadOnlySet<string>>(
                cacheKey,
                async _ =>
                {
                    var roleIdMap =
                        roles.Count > 0
                            ? await userContracts.GetRoleIdsByNamesAsync(roles)
                            : new Dictionary<string, string>();

                    return await permissionContracts.GetAllPermissionsForUserAsync(
                        UserId.From(userId),
                        roleIdMap.Values.Select(id => RoleId.From(id))
                    );
                },
                CacheOptions
            ) ?? new HashSet<string>();

        var identity = new ClaimsIdentity();
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        principal.AddIdentity(identity);
        return principal;
    }
}
