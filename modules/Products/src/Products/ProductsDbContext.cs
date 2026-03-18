using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Ids;
using SimpleModule.Database;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.EntityConfigurations;

namespace SimpleModule.Products;

public class ProductsDbContext(
    DbContextOptions<ProductsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyModuleSchema("Products", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ProductId>()
            .HaveConversion<ProductId.EfCoreValueConverter, ProductId.EfCoreValueComparer>();
    }
}
