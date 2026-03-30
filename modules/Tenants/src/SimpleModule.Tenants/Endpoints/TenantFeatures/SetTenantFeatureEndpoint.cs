using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.TenantFeatures;

public class SetTenantFeatureEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/{id}/features/{flagName}",
                async (TenantId id, string flagName, SetTenantFeatureRequest request, HttpContext context) =>
                {
                    var featureFlags = context.RequestServices.GetService<IFeatureFlagContracts>();
                    if (featureFlags is null)
                    {
                        return Results.NotFound();
                    }

                    var result = await featureFlags.SetOverrideAsync(
                        flagName,
                        new SetOverrideRequest
                        {
                            OverrideType = OverrideType.Tenant,
                            OverrideValue = id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            IsEnabled = request.IsEnabled,
                        }
                    );

                    return Results.Ok(result);
                }
            )
            .RequirePermission(TenantsPermissions.Update);
}

public class SetTenantFeatureRequest
{
    public bool IsEnabled { get; set; }
}
