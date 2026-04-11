using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    public DbSet<PageTag> Tags => Set<PageTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PageConfiguration());
        modelBuilder.ApplyConfiguration(new PageTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new PageTagConfiguration());
        modelBuilder.ApplyModuleSchema("PageBuilder", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<PageId>()
            .HaveConversion<PageId.EfCoreValueConverter, PageId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<PageTemplateId>()
            .HaveConversion<
                PageTemplateId.EfCoreValueConverter,
                PageTemplateId.EfCoreValueComparer
            >();
        configurationBuilder
            .Properties<PageTagId>()
            .HaveConversion<PageTagId.EfCoreValueConverter, PageTagId.EfCoreValueComparer>();

        // SQLite cannot ORDER BY or compare DateTimeOffset expressions natively.
        // Store as long (binary ticks) only when running against SQLite; leave
        // other providers to their native timestamptz/datetimeoffset types.
        if (dbOptions.Value.DetectProvider("PageBuilder") == DatabaseProvider.Sqlite)
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
