using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Views;

[ViewPage("Tenants/Features")]
public class FeaturesEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/{id}/features",
                async (
                    TenantId id,
                    ITenantContracts contracts,
                    IFeatureFlagContracts? featureFlags
                ) =>
                {
                    var tenant = await contracts.GetTenantByIdAsync(id);
                    if (tenant is null)
                    {
                        return Results.NotFound();
                    }

                    if (featureFlags is null)
                    {
                        return Inertia.Render(
                            "Tenants/Features",
                            new { tenant, flags = Array.Empty<FeatureFlag>(), tenantOverrides = Array.Empty<FeatureFlagOverride>() }
                        );
                    }

                    var flags = (await featureFlags.GetAllFlagsAsync()).ToList();
                    var tenantIdStr = id.Value.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    var tenantOverrides = new List<FeatureFlagOverride>();
                    foreach (var flag in flags)
                    {
                        var overrides = await featureFlags.GetOverridesAsync(flag.Name);
                        tenantOverrides.AddRange(
                            overrides.Where(o =>
                                o.OverrideType == OverrideType.Tenant
                                && o.OverrideValue == tenantIdStr
                            )
                        );
                    }

                    return Inertia.Render(
                        "Tenants/Features",
                        new { tenant, flags, tenantOverrides }
                    );
                }
            )
            .RequirePermission(TenantsPermissions.View);
    }
}
