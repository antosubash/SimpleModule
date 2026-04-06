using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Events;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.FeatureFlags.Contracts.Events;

namespace SimpleModule.FeatureFlags.Endpoints.FeatureFlags;

public class UpdateEndpoint : IEndpoint
{
    public const string Route = FeatureFlagsConstants.Routes.Update;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    string name,
                    UpdateFeatureFlagRequest request,
                    IFeatureFlagContracts featureFlags,
                    IEventBus eventBus,
                    ClaimsPrincipal user
                ) =>
                {
                    var flag = await featureFlags.UpdateFlagAsync(name, request);
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
                    await eventBus.PublishAsync(
                        new FeatureFlagToggledEvent(name, request.IsEnabled, userId)
                    );
                    return TypedResults.Ok(flag);
                }
            )
            .RequirePermission(FeatureFlagsPermissions.Manage);
}
