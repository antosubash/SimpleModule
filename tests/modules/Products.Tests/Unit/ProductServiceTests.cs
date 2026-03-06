using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Products;
using SimpleModule.Products.Contracts;

namespace Products.Tests.Unit;

public sealed class ProductServiceTests : IDisposable
{
    private readonly ProductsDbContext _db;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProductsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Products"] = "Data Source=:memory:",
                },
            }
        );
        _db = new ProductsDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new ProductService(_db, NullLogger<ProductService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllProductsAsync_ReturnsNonEmptyCollection()
    {
        var products = await _sut.GetAllProductsAsync();

        products.Should().NotBeEmpty();
        products
            .Should()
            .AllSatisfy(p =>
            {
                p.Id.Should().BeGreaterThan(0);
                p.Name.Should().NotBeNullOrWhiteSpace();
                p.Price.Should().BeGreaterThan(0);
            });
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsProductWithMatchingId()
    {
        var product = await _sut.GetProductByIdAsync(1);

        product.Should().NotBeNull();
        product!.Id.Should().Be(1);
    }
}
