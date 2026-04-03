using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.RateLimiting;

namespace SimpleModule.Hosting.RateLimiting;

public static class RateLimitingSetup
{
    public static IServiceCollection AddSimpleModuleRateLimiting(
        this IServiceCollection services,
        RateLimitPolicyRegistry registry
    )
    {
        services.AddSingleton<IRateLimitPolicyRegistry>(registry);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out var retryAfter
                )
                    ? ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture)
                    : "60";

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsync(
                    """{"type":"https://httpstatuses.io/429","title":"Too Many Requests","status":429,"detail":"Rate limit exceeded. Please retry after the period indicated in the Retry-After header."}""",
                    cancellationToken
                );
            };

            foreach (var policy in registry.GetPolicies())
            {
                options.AddPolicy(policy.Name, context => CreatePartition(context, policy));
            }
        });

        return services;
    }

    public static WebApplication UseSimpleModuleRateLimiting(this WebApplication app)
    {
        app.UseMiddleware<RateLimitHeaderMiddleware>();
        app.UseRateLimiter();
        return app;
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext context,
        RateLimitPolicyDefinition policy
    )
    {
        var key = ResolvePartitionKey(context, policy.Target);

        return policy.PolicyType switch
        {
            RateLimitPolicyType.FixedWindow => RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    Window = policy.Window,
                    QueueLimit = policy.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                }
            ),
            RateLimitPolicyType.SlidingWindow => RateLimitPartition.GetSlidingWindowLimiter(
                key,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    Window = policy.Window,
                    SegmentsPerWindow = policy.SegmentsPerWindow,
                    QueueLimit = policy.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                }
            ),
            RateLimitPolicyType.TokenBucket => RateLimitPartition.GetTokenBucketLimiter(
                key,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = policy.TokenLimit,
                    TokensPerPeriod = policy.TokensPerPeriod,
                    ReplenishmentPeriod = policy.ReplenishmentPeriod,
                    QueueLimit = policy.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                }
            ),
            _ => RateLimitPartition.GetNoLimiter(key),
        };
    }

    private static string ResolvePartitionKey(HttpContext context, RateLimitTarget target)
    {
        return target switch
        {
            RateLimitTarget.Ip => context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            RateLimitTarget.User => context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "anonymous",
            RateLimitTarget.IpAndUser =>
                $"{context.Connection.RemoteIpAddress}:{context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"}",
            RateLimitTarget.Global => "__global__",
            _ => context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        };
    }
}
