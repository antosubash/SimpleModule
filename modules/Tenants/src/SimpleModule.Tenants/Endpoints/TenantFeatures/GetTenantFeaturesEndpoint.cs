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
                    var tenantOverrides = await TenantFeatureHelper.GetOverridesForTenantAsync(
                        featureFlags,
                        flags,
                        id
                    );

                    return Results.Ok(new TenantFeaturesResponse(flags, tenantOverrides));
                }
            )
            .RequirePermission(TenantsPermissions.View);
}

public sealed record TenantFeaturesResponse(
    IEnumerable<FeatureFlag> Flags,
    IEnumerable<FeatureFlagOverride> TenantOverrides
);
