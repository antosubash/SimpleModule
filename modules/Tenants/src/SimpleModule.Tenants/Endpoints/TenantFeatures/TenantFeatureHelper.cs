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
        var overrideTasks = flags
            .Where(f => !f.IsDeprecated)
            .Select(f => featureFlags.GetOverridesAsync(f.Name));
        var allOverrides = await Task.WhenAll(overrideTasks);
        return allOverrides
            .SelectMany(o => o)
            .Where(o =>
                o.OverrideType == OverrideType.Tenant
                && string.Equals(o.OverrideValue, tenantIdStr, StringComparison.Ordinal)
            )
            .ToList();
    }
}
