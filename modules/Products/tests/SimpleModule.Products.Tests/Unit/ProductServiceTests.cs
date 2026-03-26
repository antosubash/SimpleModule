using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Exceptions;
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
    public async Task GetAllProductsAsync_ReturnsAllProducts()
    {
        var products = await _sut.GetAllProductsAsync();

        products.Should().NotBeEmpty();
        products
            .Should()
            .AllSatisfy(p =>
            {
                p.Id.Value.Should().BeGreaterThan(0);
                p.Name.Should().NotBeNullOrWhiteSpace();
                p.Price.Should().BeGreaterThan(0);
            });
    }

    [Fact]
    public async Task GetProductByIdAsync_WithExistingId_ReturnsProduct()
    {
        var product = await _sut.GetProductByIdAsync(ProductId.From(1));

        product.Should().NotBeNull();
        product!.Id.Should().Be(ProductId.From(1));
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var product = await _sut.GetProductByIdAsync(ProductId.From(99999));

        product.Should().BeNull();
    }

    [Fact]
    public async Task CreateProductAsync_CreatesAndReturnsProduct()
    {
        var request = new CreateProductRequest { Name = "Test Widget", Price = 19.99m };

        var product = await _sut.CreateProductAsync(request);

        product.Should().NotBeNull();
        product.Name.Should().Be("Test Widget");
        product.Price.Should().Be(19.99m);
        product.Id.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateProductAsync_WithValidData_UpdatesProduct()
    {
        var createRequest = new CreateProductRequest { Name = "Original", Price = 10.00m };
        var created = await _sut.CreateProductAsync(createRequest);

        var updateRequest = new UpdateProductRequest { Name = "Updated", Price = 20.00m };
        var updated = await _sut.UpdateProductAsync(created.Id, updateRequest);

        updated.Name.Should().Be("Updated");
        updated.Price.Should().Be(20.00m);
    }

    [Fact]
    public async Task UpdateProductAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var request = new UpdateProductRequest { Name = "Test", Price = 10.00m };

        var act = () => _sut.UpdateProductAsync(ProductId.From(99999), request);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Product*99999*not found*");
    }

    [Fact]
    public async Task DeleteProductAsync_WithExistingId_RemovesProduct()
    {
        var createRequest = new CreateProductRequest { Name = "ToDelete", Price = 5.00m };
        var created = await _sut.CreateProductAsync(createRequest);

        await _sut.DeleteProductAsync(created.Id);

        var found = await _sut.GetProductByIdAsync(created.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProductAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var act = () => _sut.DeleteProductAsync(ProductId.From(99999));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Product*99999*not found*");
    }
}
