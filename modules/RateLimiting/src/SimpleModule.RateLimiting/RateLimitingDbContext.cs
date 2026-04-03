using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.RateLimiting.Contracts;
using SimpleModule.RateLimiting.EntityConfigurations;

namespace SimpleModule.RateLimiting;

public class RateLimitingDbContext(
    DbContextOptions<RateLimitingDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<RateLimitRule> Rules => Set<RateLimitRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RateLimitRuleConfiguration());
        modelBuilder.ApplyModuleSchema("RateLimiting", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<RateLimitRuleId>()
            .HaveConversion<
                RateLimitRuleId.EfCoreValueConverter,
                RateLimitRuleId.EfCoreValueComparer
            >();
    }
}
