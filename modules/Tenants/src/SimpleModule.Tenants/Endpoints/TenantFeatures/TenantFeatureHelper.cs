using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.TenantFeatures;

internal static class TenantFeatureHelper
{
    public static async Task<List<FeatureFlagOverride>> GetOverridesForTenantAsync(
        IFeatureFlagContracts featureFlags,
        IEnumerable<FeatureFlag> flags,
        TenantId tenantId
    )
    {
        var tenantIdStr = tenantId.Value.ToString(
            System.Globalization.CultureInfo.InvariantCulture
        );

        // Await sequentially, not via Task.WhenAll: every GetOverridesAsync call
        // hits the same scoped FeatureFlagsDbContext, and EF Core forbids
        // concurrent operations on one context instance. With 2+ active flags
        // the parallel version reliably threw "A second operation was started
        // on this context instance..." → HTTP 500 (same class as #242).
        var result = new List<FeatureFlagOverride>();
        foreach (var flag in flags.Where(f => !f.IsDeprecated))
        {
            var overrides = await featureFlags.GetOverridesAsync(flag.Name);
            result.AddRange(
                overrides.Where(o =>
                    o.OverrideType == OverrideType.Tenant
                    && string.Equals(o.OverrideValue, tenantIdStr, StringComparison.Ordinal)
                )
            );
        }

        return result;
    }
}
