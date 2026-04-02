using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Agents.Sessions;
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
}
