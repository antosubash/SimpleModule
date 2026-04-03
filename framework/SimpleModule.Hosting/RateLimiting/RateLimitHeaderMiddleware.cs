using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using SimpleModule.Core.RateLimiting;

namespace SimpleModule.Hosting.RateLimiting;

public sealed class RateLimitHeaderMiddleware(
    RequestDelegate next,
    IRateLimitPolicyRegistry registry
)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var rateLimitMetadata = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();

        if (rateLimitMetadata is { PolicyName: { } policyName })
        {
            var policy = registry.GetPolicy(policyName);
            if (policy is not null)
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode != StatusCodes.Status429TooManyRequests)
                    {
                        context.Response.Headers["X-RateLimit-Policy"] = policy.Name;
                        context.Response.Headers["X-RateLimit-Limit"] =
                            policy.PolicyType == RateLimitPolicyType.TokenBucket
                                ? policy.TokenLimit.ToString(CultureInfo.InvariantCulture)
                                : policy.PermitLimit.ToString(CultureInfo.InvariantCulture);
                    }

                    return Task.CompletedTask;
                });
            }
        }

        await next(context);
    }
}
