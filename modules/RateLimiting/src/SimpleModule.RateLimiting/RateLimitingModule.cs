using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.RateLimiting;
using SimpleModule.Database;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting;

[Module(
    RateLimitingConstants.ModuleName,
    RoutePrefix = RateLimitingConstants.RoutePrefix,
    ViewPrefix = RateLimitingConstants.ViewPrefix
)]
public class RateLimitingModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<RateLimitingDbContext>(
            configuration,
            RateLimitingConstants.ModuleName
        );
    }

    public void ConfigureRateLimits(IRateLimitBuilder builder)
    {
        builder
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = "fixed-default",
                    PolicyType = RateLimitPolicyType.FixedWindow,
                    Target = RateLimitTarget.Ip,
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                }
            )
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = "sliding-strict",
                    PolicyType = RateLimitPolicyType.SlidingWindow,
                    Target = RateLimitTarget.IpAndUser,
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                }
            )
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = "token-bucket",
                    PolicyType = RateLimitPolicyType.TokenBucket,
                    Target = RateLimitTarget.Ip,
                    TokenLimit = 100,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                }
            )
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = "auth-strict",
                    PolicyType = RateLimitPolicyType.FixedWindow,
                    Target = RateLimitTarget.Ip,
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                }
            );
    }

    // Menu items removed — accessible via Admin hub page
}
