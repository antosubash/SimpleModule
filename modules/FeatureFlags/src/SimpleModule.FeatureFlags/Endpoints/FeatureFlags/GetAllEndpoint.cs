using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Endpoints.FeatureFlags;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = FeatureFlagsConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (IFeatureFlagContracts featureFlags) =>
                {
                    var flags = await featureFlags.GetAllFlagsAsync();
                    return TypedResults.Ok(flags);
                }
            )
            .RequirePermission(FeatureFlagsPermissions.View);
}
