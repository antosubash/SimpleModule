using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Pages;

public class ManageEndpoint : IViewEndpoint
{
    public const string Route = FeatureFlagsConstants.Routes.Manage;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (IFeatureFlagContracts featureFlags) =>
                {
                    var flags = await featureFlags.GetAllFlagsAsync();
                    return Inertia.Render("FeatureFlags/Manage", new { flags });
                }
            )
            .RequirePermission(FeatureFlagsPermissions.View);
    }
}
