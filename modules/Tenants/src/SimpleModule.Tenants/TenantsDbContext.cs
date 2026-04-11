using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Tenants.Contracts;
using SimpleModule.Tenants.EntityConfigurations;

namespace SimpleModule.Tenants;

public class TenantsDbContext(
    DbContextOptions<TenantsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<TenantHostEntity> TenantHosts => Set<TenantHostEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new TenantHostConfiguration());
        modelBuilder.ApplyModuleSchema("Tenants", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<TenantId>()
            .HaveConversion<TenantId.EfCoreValueConverter, TenantId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<TenantHostId>()
            .HaveConversion<TenantHostId.EfCoreValueConverter, TenantHostId.EfCoreValueComparer>();
    }
}
