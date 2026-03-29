using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.Core.Inertia;

namespace SimpleModule.FeatureFlags.Middleware;

public sealed class FeatureFlagMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sharedData = context.RequestServices.GetService<InertiaSharedData>();
        var featureFlagService = context.RequestServices.GetService<IFeatureFlagService>();

        if (sharedData is not null && featureFlagService is not null)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var flags = await featureFlagService.GetAllEnabledAsync(userId, roles);

            sharedData.Set("featureFlags", flags);
        }

        await next(context);
    }
}
