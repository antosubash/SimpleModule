using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Extensions;

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

                var userId = context.HttpContext.User.GetUserId();
                var roles = context.HttpContext.User.GetRoles();

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
