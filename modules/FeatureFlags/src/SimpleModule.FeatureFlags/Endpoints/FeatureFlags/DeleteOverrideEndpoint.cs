using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Endpoints.FeatureFlags;

public class DeleteOverrideEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/overrides/{id:int}",
                async (int id, IFeatureFlagContracts featureFlags) =>
                {
                    await featureFlags.DeleteOverrideAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(FeatureFlagsPermissions.Manage);
}
