using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.EntityConfigurations;
using SimpleModule.Database;

namespace SimpleModule.AuditLogs;

public class AuditLogsDbContext(
    DbContextOptions<AuditLogsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());
        modelBuilder.ApplyModuleSchema(AuditLogsConstants.ModuleName, dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<AuditEntryId>()
            .HaveConversion<AuditEntryId.EfCoreValueConverter, AuditEntryId.EfCoreValueComparer>();
    }
}
