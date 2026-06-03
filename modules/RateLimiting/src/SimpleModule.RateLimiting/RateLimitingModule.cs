using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        services.AddValidatorsFromAssemblyContaining<RateLimitingModule>();
        services.AddSingleton<RateLimitRuleCache>();
        services.AddSingleton<IRateLimitRuleSource>(sp =>
            sp.GetRequiredService<RateLimitRuleCache>()
        );
        services.AddHostedService(sp => sp.GetRequiredService<RateLimitRuleCache>());
    }

    public void ConfigureHost(IHost host)
    {
        // Ensure the RateLimiting_Rules table exists before the rule cache's
        // hosted service queries it on startup (dev convenience; prod uses
        // migrations). ConfigureHost runs in the StartingAsync lifecycle phase,
        // strictly before any IHostedService.StartAsync, so the table is present
        // by the time RateLimitRuleCache reads it. Mirrors BackgroundJobsModule.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RateLimitingDbContext>();
        if (!db.Database.EnsureCreated())
        {
            try
            {
                db.GetService<IRelationalDatabaseCreator>()?.CreateTables();
            }
#pragma warning disable CA1031
            catch
            { /* tables already exist */
            }
#pragma warning restore CA1031
        }
    }

    public void ConfigureRateLimits(IRateLimitBuilder builder)
    {
        builder
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = RateLimitPolicies.FixedDefault,
                    PolicyType = RateLimitPolicyType.FixedWindow,
                    Target = RateLimitTarget.Ip,
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                }
            )
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = RateLimitPolicies.SlidingStrict,
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
                    Name = RateLimitPolicies.TokenBucket,
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
                    Name = RateLimitPolicies.AuthStrict,
                    PolicyType = RateLimitPolicyType.FixedWindow,
                    Target = RateLimitTarget.Ip,
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                }
            );
    }

    // Menu items removed — accessible via Admin hub page
}
