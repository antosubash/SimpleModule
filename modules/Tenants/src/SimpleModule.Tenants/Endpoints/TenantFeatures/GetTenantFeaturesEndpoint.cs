using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.TenantFeatures;

public class GetTenantFeaturesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}/features",
                async (TenantId id, HttpContext context) =>
                {
                    var featureFlags = context.RequestServices.GetService<IFeatureFlagContracts>();
                    if (featureFlags is null)
                    {
                        return Results.Ok(
                            new TenantFeaturesResponse([], [])
                        );
                    }

                    var flags = await featureFlags.GetAllFlagsAsync();
                    var allOverrides = new List<FeatureFlagOverride>();
                    var tenantIdStr = id.Value.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    );

                    foreach (var flag in flags)
                    {
                        var overrides = await featureFlags.GetOverridesAsync(flag.Name);
                        allOverrides.AddRange(
                            overrides.Where(o =>
                                o.OverrideType == OverrideType.Tenant
                                && string.Equals(o.OverrideValue, tenantIdStr, StringComparison.Ordinal)
                            )
                        );
                    }

                    return Results.Ok(new TenantFeaturesResponse(flags, allOverrides));
                }
            )
            .RequirePermission(TenantsPermissions.View);
}

public sealed record TenantFeaturesResponse(
    IEnumerable<FeatureFlag> Flags,
    IEnumerable<FeatureFlagOverride> TenantOverrides
);
