using Microsoft.AspNetCore.Builder;

namespace SimpleModule.Core.RateLimiting;

public static class EndpointRateLimitExtensions
{
    public static TBuilder RateLimit<TBuilder>(this TBuilder builder, string policyName)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireRateLimiting(policyName);
        return builder;
    }
}
