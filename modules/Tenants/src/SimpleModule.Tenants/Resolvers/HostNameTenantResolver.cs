using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Caching;

namespace SimpleModule.Tenants.Resolvers;

public sealed class HostNameTenantResolver(TenantsDbContext db, ICacheStore cache)
{
    private static readonly CacheEntryOptions CacheOptions = CacheEntryOptions.Expires(
        TimeSpan.FromMinutes(5)
    );

    public async Task<string?> ResolveAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
        {
            return null;
        }

        var cacheKey = $"tenant:host:{host}";
        return await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
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
