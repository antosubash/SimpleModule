using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.PageBuilder.Contracts;
using SimpleModule.PageBuilder.EntityConfigurations;

namespace SimpleModule.PageBuilder;

public class PageBuilderDbContext(
    DbContextOptions<PageBuilderDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<Page> Pages => Set<Page>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PageConfiguration());
        modelBuilder.ApplyModuleSchema("PageBuilder", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<PageId>()
            .HaveConversion<PageId.EfCoreValueConverter, PageId.EfCoreValueComparer>();
    }
}
