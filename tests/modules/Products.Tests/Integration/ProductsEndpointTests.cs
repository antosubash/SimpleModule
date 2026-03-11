using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Products.Tests.Integration;

public class ProductsEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllProducts_Returns200WithProductList()
    {
        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProductById_Returns200WithProduct()
    {
        var response = await _client.GetAsync("/api/products/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(1);
    }
}
