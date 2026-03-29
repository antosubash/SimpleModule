using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Views;

[ViewPage("FeatureFlags/Manage")]
public class ManageEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/",
                async (IFeatureFlagContracts featureFlags) =>
                {
                    var flags = await featureFlags.GetAllFlagsAsync();
                    return Inertia.Render("FeatureFlags/Manage", new { flags });
                }
            )
            .RequirePermission(FeatureFlagsPermissions.View);
    }
}
