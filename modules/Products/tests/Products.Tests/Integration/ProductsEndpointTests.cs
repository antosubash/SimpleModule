using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Core.Ids;
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
        product!.Id.Should().Be(ProductId.From(1));
    }

    [Fact]
    public async Task CreateProduct_Returns201()
    {
        var request = new CreateProductRequest { Name = "New Product", Price = 29.99m };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("New Product");
        product.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistentId_Returns404()
    {
        var request = new UpdateProductRequest { Name = "Updated", Price = 10.00m };

        var response = await _client.PutAsJsonAsync("/api/products/99999", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_WithExistingId_Returns204()
    {
        // Create a product first
        var createRequest = new CreateProductRequest { Name = "ToDelete", Price = 5.00m };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();

        var response = await _client.DeleteAsync($"/api/products/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
