using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Endpoints.FeatureFlags;

public class CheckEndpoint : IEndpoint
{
    public const string Route = FeatureFlagsConstants.Routes.Check;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (string name, IFeatureFlagContracts featureFlags, ClaimsPrincipal user) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);
                    var isEnabled = await featureFlags.IsEnabledAsync(name, userId, roles);
                    return TypedResults.Ok(new { name, isEnabled });
                }
            )
            .RequireAuthorization();
}
