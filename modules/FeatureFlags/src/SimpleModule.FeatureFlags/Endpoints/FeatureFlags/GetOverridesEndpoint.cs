using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Endpoints.FeatureFlags;

public class GetOverridesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{name}/overrides",
                async (string name, IFeatureFlagContracts featureFlags) =>
                {
                    var overrides = await featureFlags.GetOverridesAsync(name);
                    return TypedResults.Ok(overrides);
                }
            )
            .RequirePermission(FeatureFlagsPermissions.View);
}
