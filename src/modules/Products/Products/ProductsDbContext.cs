using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

public class ProductsDbContext(
    DbContextOptions<ProductsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Product>().HasData(GenerateSeedProducts());

        modelBuilder.ApplyModuleSchema("Products", dbOptions.Value);
    }

    private static Product[] GenerateSeedProducts()
    {
        var id = 0;
        var faker = new Faker<Product>()
            .UseSeed(54321)
            .RuleFor(p => p.Id, _ => ++id)
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(
                p => p.Price,
                f =>
                    decimal.Parse(
                        f.Commerce.Price(10, 1000),
                        System.Globalization.CultureInfo.InvariantCulture
                    )
            );

        return faker.Generate(10).ToArray();
    }
}
