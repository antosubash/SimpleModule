using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace SimpleModule.Tenants.Resolvers;

public sealed class HostNameTenantResolver(
    TenantsDbContext db,
    IMemoryCache cache
)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<string?> ResolveAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
        {
            return null;
        }

        var cacheKey = $"tenant:host:{host}";
        if (cache.TryGetValue(cacheKey, out string? cachedTenantId))
        {
            return cachedTenantId;
        }

        var tenantHost = await db
            .TenantHosts.AsNoTracking()
            .Where(h => h.HostName == host && h.IsActive)
            .Select(h => (int?)h.TenantId.Value)
            .FirstOrDefaultAsync();

        if (tenantHost is null)
        {
            cache.Set(cacheKey, (string?)null, CacheDuration);
            return null;
        }

        var tenantId = tenantHost.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        cache.Set(cacheKey, tenantId, CacheDuration);
        return tenantId;
    }
}
