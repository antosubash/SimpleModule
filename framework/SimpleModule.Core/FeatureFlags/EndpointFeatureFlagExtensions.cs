using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core.FeatureFlags;

public static class EndpointFeatureFlagExtensions
{
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string featureName)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(
            async (context, next) =>
            {
                var featureFlagService =
                    context.HttpContext.RequestServices.GetService<IFeatureFlagService>();

                // If the service is not registered, allow the request (feature enabled by default)
                if (featureFlagService is null)
                {
                    return await next(context);
                }

                var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var roles = context.HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value);

                var isEnabled = await featureFlagService.IsEnabledAsync(featureName, userId, roles);

                if (!isEnabled)
                {
                    return Results.NotFound();
                }

                return await next(context);
            }
        );

        return builder;
    }
}
