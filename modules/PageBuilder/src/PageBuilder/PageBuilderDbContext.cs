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
    public DbSet<PageTemplate> Templates => Set<PageTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PageConfiguration());
        modelBuilder.ApplyConfiguration(new PageTemplateConfiguration());
        modelBuilder.Entity<Page>().HasQueryFilter(p => p.DeletedAt == null);
        modelBuilder.ApplyModuleSchema("PageBuilder", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<PageId>()
            .HaveConversion<PageId.EfCoreValueConverter, PageId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<PageTemplateId>()
            .HaveConversion<PageTemplateId.EfCoreValueConverter, PageTemplateId.EfCoreValueComparer>();
    }
}
