using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
                async (TenantId id, ITenantContracts contracts, HttpContext context) =>
                {
                    var tenant = await contracts.GetTenantByIdAsync(id);
                    if (tenant is null)
                    {
                        return Results.NotFound();
                    }

                    var featureFlags = context.RequestServices.GetService<IFeatureFlagContracts>();
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

                    var overrideTasks = flags
                        .Where(f => !f.IsDeprecated)
                        .Select(f => featureFlags.GetOverridesAsync(f.Name));
                    var allOverrides = await Task.WhenAll(overrideTasks);

                    var tenantOverrides = allOverrides
                        .SelectMany(o => o)
                        .Where(o =>
                            o.OverrideType == OverrideType.Tenant
                            && string.Equals(o.OverrideValue, tenantIdStr, StringComparison.Ordinal)
                        )
                        .ToList();

                    return Inertia.Render(
                        "Tenants/Features",
                        new { tenant, flags, tenantOverrides }
                    );
                }
            )
            .RequirePermission(TenantsPermissions.View);
    }
}
