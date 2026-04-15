using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.FeatureFlags.Contracts.Events;
using Wolverine;

namespace SimpleModule.FeatureFlags.Endpoints.FeatureFlags;

public class SetOverrideEndpoint : IEndpoint
{
    public const string Route = FeatureFlagsConstants.Routes.SetOverride;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    string name,
                    SetOverrideRequest request,
                    IFeatureFlagContracts featureFlags,
                    IMessageBus bus
                ) =>
                {
                    var result = await featureFlags.SetOverrideAsync(name, request);
                    await bus.PublishAsync(
                        new FeatureFlagOverrideChangedEvent(
                            name,
                            OverrideAction.Set,
                            request.OverrideType,
                            request.OverrideValue
                        )
                    );
                    return TypedResults.Created(
                        $"{FeatureFlagsConstants.RoutePrefix}/{name}/overrides",
                        result
                    );
                }
            )
            .RequirePermission(FeatureFlagsPermissions.Manage);
}
