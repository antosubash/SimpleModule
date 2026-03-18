using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.EntityConfigurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.Name).IsRequired();
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");

        builder.HasData(GenerateSeedProducts());
    }

    private static Product[] GenerateSeedProducts()
    {
        var id = 0;
        var faker = new Faker<Product>()
            .UseSeed(54321)
            .RuleFor(p => p.Id, _ => ProductId.From(++id))
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
