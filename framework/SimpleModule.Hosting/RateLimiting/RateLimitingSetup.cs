using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.RateLimiting;

namespace SimpleModule.Hosting.RateLimiting;

public static class RateLimitingSetup
{
    private const string NoDbRulePartitionKey = "__no_db_rule__";
    private const string GlobalPartitionKey = "__global__";
    private const string UnknownIpPartitionKey = "unknown";
    private const string AnonymousUserPartitionKey = "anonymous";

    public static IServiceCollection AddSimpleModuleRateLimiting(
        this IServiceCollection services,
        IRateLimitPolicyRegistry registry
    )
    {
        services.AddSingleton(registry);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = RateLimitRejectionHandler.HandleAsync;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var source = context.RequestServices.GetService<IRateLimitRuleSource>();
                var policy = source?.FindForPath(context.Request.Path);
                return policy is null
                    ? RateLimitPartition.GetNoLimiter(NoDbRulePartitionKey)
                    : CreatePartition(context, policy);
            });

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
            RateLimitTarget.Ip => context.Connection.RemoteIpAddress?.ToString()
                ?? UnknownIpPartitionKey,
            RateLimitTarget.User => context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? AnonymousUserPartitionKey,
            RateLimitTarget.IpAndUser =>
                $"{context.Connection.RemoteIpAddress}:{context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? AnonymousUserPartitionKey}",
            RateLimitTarget.Global => GlobalPartitionKey,
            _ => context.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartitionKey,
        };
    }

    internal static class RateLimitRejectionHandler
    {
        private const string JsonProblemBody =
            """{"type":"https://httpstatuses.io/429","title":"Too Many Requests","status":429,"detail":"Rate limit exceeded. Please retry after the period indicated in the Retry-After header."}""";

        private const string HtmlBody = """
            <!DOCTYPE html>
            <html lang="en"><head><meta charset="utf-8"><title>429 Too Many Requests</title>
            <style>body{font-family:system-ui,sans-serif;max-width:32rem;margin:4rem auto;padding:0 1rem;color:#1f2937}h1{margin-bottom:.5rem}</style>
            </head><body>
            <h1>Too many requests</h1>
            <p>You have hit the rate limit for this endpoint. Please wait and try again.</p>
            </body></html>
            """;

        public static async ValueTask HandleAsync(
            OnRejectedContext context,
            CancellationToken cancellationToken
        )
        {
            var response = context.HttpContext.Response;
            response.Headers["Retry-After"] = context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter
            )
                ? ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture)
                : "60";

            if (PrefersHtml(context.HttpContext.Request))
            {
                response.ContentType = "text/html; charset=utf-8";
                await response.WriteAsync(HtmlBody, cancellationToken);
            }
            else
            {
                response.ContentType = "application/problem+json";
                await response.WriteAsync(JsonProblemBody, cancellationToken);
            }
        }

        private static bool PrefersHtml(HttpRequest request)
        {
            // Inertia AJAX requests expect JSON even though they originate
            // from a browser, so this check must come before the Accept sniff.
            if (request.IsInertia())
            {
                return false;
            }

            var accept = request.Headers.Accept.ToString();
            return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
        }
    }
}
