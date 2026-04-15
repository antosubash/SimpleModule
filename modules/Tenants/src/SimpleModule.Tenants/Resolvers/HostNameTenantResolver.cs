using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.Tenants.Resolvers;

public sealed class HostNameTenantResolver(TenantsDbContext db, IFusionCache cache)
{
    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = TimeSpan.FromMinutes(5),
    };

    public async Task<string?> ResolveAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
        {
            return null;
        }

        var cacheKey = $"tenant:host:{host}";
        return await cache.GetOrSetAsync<string?>(
            cacheKey,
            async (_, ct) =>
            {
                var tenantHost = await db
                    .TenantHosts.AsNoTracking()
                    .Where(h => h.HostName == host && h.IsActive)
                    .Select(h => (int?)h.TenantId.Value)
                    .FirstOrDefaultAsync(ct);

                return tenantHost?.ToString(System.Globalization.CultureInfo.InvariantCulture);
            },
            CacheOptions
        );
    }
}
