using FluentAssertions;
using SimpleModule.Products;
using SimpleModule.Products.Contracts;

namespace Products.Tests.Unit;

public class ProductServiceTests
{
    private readonly ProductService _sut = new();

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
        var product = await _sut.GetProductByIdAsync(42);

        product.Should().NotBeNull();
        product!.Id.Should().Be(42);
    }
}
