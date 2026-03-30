using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace SimpleModule.Tenants.Resolvers;

public sealed class HostNameTenantResolver(
    TenantsDbContext db,
    IMemoryCache cache
)
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    public async Task<string?> ResolveAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
        {
            return null;
        }

        var cacheKey = $"tenant:host:{host}";
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetOptions(CacheOptions);

            var tenantHost = await db
                .TenantHosts.AsNoTracking()
                .Where(h => h.HostName == host && h.IsActive)
                .Select(h => (int?)h.TenantId.Value)
                .FirstOrDefaultAsync();

            return tenantHost?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        });
    }
}
