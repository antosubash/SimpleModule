using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Endpoints.FeatureFlags;

public class DeleteOverrideEndpoint : IEndpoint
{
    public const string Route = FeatureFlagsConstants.Routes.DeleteOverride;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async (int id, IFeatureFlagContracts featureFlags) =>
                {
                    await featureFlags.DeleteOverrideAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(FeatureFlagsPermissions.Manage);
}
