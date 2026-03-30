using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Endpoints.FeatureFlags;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                async (IFeatureFlagContracts featureFlags) =>
                {
                    var flags = await featureFlags.GetAllFlagsAsync();
                    return TypedResults.Ok(flags);
                }
            )
            .RequirePermission(FeatureFlagsPermissions.View);
}
