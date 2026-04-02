using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.FileStorage.EntityConfigurations;

namespace SimpleModule.FileStorage;

public class FileStorageDbContext(
    DbContextOptions<FileStorageDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StoredFileConfiguration());
        modelBuilder.ApplyModuleSchema("FileStorage", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<FileStorageId>()
            .HaveConversion<
                FileStorageId.EfCoreValueConverter,
                FileStorageId.EfCoreValueComparer
            >();
    }
}
