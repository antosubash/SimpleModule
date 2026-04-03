using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.RateLimiting;

namespace SimpleModule.Hosting.RateLimiting;

public static class RateLimitingSetup
{
    public static IServiceCollection AddSimpleModuleRateLimiting(
        this IServiceCollection services,
        IRateLimitPolicyRegistry registry
    )
    {
        services.AddSingleton(registry);

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
                RegisterPolicy(options, policy);
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

    private static void RegisterPolicy(RateLimiterOptions options, RateLimitPolicyDefinition policy)
    {
        switch (policy.PolicyType)
        {
            case RateLimitPolicyType.FixedWindow:
            {
                var limiterOptions = new FixedWindowRateLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    Window = policy.Window,
                    QueueLimit = policy.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                };
                options.AddPolicy(
                    policy.Name,
                    context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            ResolvePartitionKey(context, policy.Target),
                            _ => limiterOptions
                        )
                );
                break;
            }
            case RateLimitPolicyType.SlidingWindow:
            {
                var limiterOptions = new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    Window = policy.Window,
                    SegmentsPerWindow = policy.SegmentsPerWindow,
                    QueueLimit = policy.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                };
                options.AddPolicy(
                    policy.Name,
                    context =>
                        RateLimitPartition.GetSlidingWindowLimiter(
                            ResolvePartitionKey(context, policy.Target),
                            _ => limiterOptions
                        )
                );
                break;
            }
            case RateLimitPolicyType.TokenBucket:
            {
                var limiterOptions = new TokenBucketRateLimiterOptions
                {
                    TokenLimit = policy.TokenLimit,
                    TokensPerPeriod = policy.TokensPerPeriod,
                    ReplenishmentPeriod = policy.ReplenishmentPeriod,
                    QueueLimit = policy.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                };
                options.AddPolicy(
                    policy.Name,
                    context =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            ResolvePartitionKey(context, policy.Target),
                            _ => limiterOptions
                        )
                );
                break;
            }
            default:
                options.AddPolicy(
                    policy.Name,
                    context =>
                        RateLimitPartition.GetNoLimiter(ResolvePartitionKey(context, policy.Target))
                );
                break;
        }
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
