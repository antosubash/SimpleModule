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
                        return Results.Ok(new TenantFeaturesResponse([], []));
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

                    return Results.Ok(new TenantFeaturesResponse(flags, tenantOverrides));
                }
            )
            .RequirePermission(TenantsPermissions.View);
}

public sealed record TenantFeaturesResponse(
    IEnumerable<FeatureFlag> Flags,
    IEnumerable<FeatureFlagOverride> TenantOverrides
);
