using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Admin.Contracts;
using SimpleModule.Admin.Entities;
using SimpleModule.Database;

namespace SimpleModule.Admin;

public class AdminDbContext(
    DbContextOptions<AdminDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
        modelBuilder.ApplyModuleSchema(AdminConstants.ModuleName, dbOptions.Value);
    }
}
