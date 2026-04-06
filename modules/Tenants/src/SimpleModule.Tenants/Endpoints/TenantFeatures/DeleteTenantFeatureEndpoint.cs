using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.TenantFeatures;

public class DeleteTenantFeatureEndpoint : IEndpoint
{
    public const string Route = TenantsConstants.Routes.Api.DeleteTenantFeature;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async (TenantId id, string flagName, HttpContext context) =>
                {
                    var featureFlags = context.RequestServices.GetService<IFeatureFlagContracts>();
                    if (featureFlags is null)
                    {
                        return Results.NotFound();
                    }

                    var tenantIdStr = id.Value.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    var overrides = await featureFlags.GetOverridesAsync(flagName);
                    var tenantOverride = overrides.FirstOrDefault(o =>
                        o.OverrideType == OverrideType.Tenant
                        && string.Equals(o.OverrideValue, tenantIdStr, StringComparison.Ordinal)
                    );

                    if (tenantOverride is null)
                    {
                        return Results.NotFound();
                    }

                    await featureFlags.DeleteOverrideAsync(tenantOverride.Id);
                    return Results.NoContent();
                }
            )
            .RequirePermission(TenantsPermissions.Update);
}
