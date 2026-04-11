using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SimpleModule.Agents.Contracts;
using SimpleModule.Database;

namespace SimpleModule.Agents.Module;

public sealed class AgentsDbContext(
    DbContextOptions<AgentsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<AgentSession> Sessions => Set<AgentSession>();
    public DbSet<AgentMessage> Messages => Set<AgentMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EntityConfigurations.AgentSessionConfiguration());
        modelBuilder.ApplyConfiguration(new EntityConfigurations.AgentMessageConfiguration());
        modelBuilder.ApplyModuleSchema(AgentsConstants.ModuleName, dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<AgentSessionId>()
            .HaveConversion<
                AgentSessionId.EfCoreValueConverter,
                AgentSessionId.EfCoreValueComparer
            >();
        configurationBuilder
            .Properties<AgentMessageId>()
            .HaveConversion<
                AgentMessageId.EfCoreValueConverter,
                AgentMessageId.EfCoreValueComparer
            >();

        // SQLite cannot ORDER BY or compare DateTimeOffset expressions natively.
        // Store as long (binary ticks) only when running against SQLite.
        if (dbOptions.Value.DetectProvider(AgentsConstants.ModuleName) == DatabaseProvider.Sqlite)
        {
            configurationBuilder
                .Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder
                .Properties<DateTimeOffset?>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }
}
