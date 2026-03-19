using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Products;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Products.Tests.Integration;

public class ProductsEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ProductsEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllProducts_WithViewPermission_Returns200WithProductList()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.View]);

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllProducts_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllProducts_WithoutPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.Create]);

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProductById_WithViewPermission_Returns200WithProduct()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.View]);

        var response = await client.GetAsync("/api/products/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(ProductId.From(1));
    }

    [Fact]
    public async Task GetProductById_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithCreatePermission_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.Create]);
        var request = new CreateProductRequest { Name = "New Product", Price = 29.99m };

        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("New Product");
        product.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task CreateProduct_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new CreateProductRequest { Name = "New Product", Price = 29.99m };

        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithoutPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.View]);
        var request = new CreateProductRequest { Name = "New Product", Price = 29.99m };

        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistentId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.Update]);
        var request = new UpdateProductRequest { Name = "Updated", Price = 10.00m };

        var response = await client.PutAsJsonAsync("/api/products/99999", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new UpdateProductRequest { Name = "Updated", Price = 10.00m };

        var response = await client.PutAsJsonAsync("/api/products/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_WithoutPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.View]);
        var request = new UpdateProductRequest { Name = "Updated", Price = 10.00m };

        var response = await client.PutAsJsonAsync("/api/products/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProduct_WithExistingId_Returns204()
    {
        var client = _factory.CreateAuthenticatedClient(
            [ProductsPermissions.Create, ProductsPermissions.Delete]
        );

        // Create a product first
        var createRequest = new CreateProductRequest { Name = "ToDelete", Price = 5.00m };
        var createResponse = await client.PostAsJsonAsync("/api/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();

        var response = await client.DeleteAsync($"/api/products/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.Delete]);

        var response = await client.DeleteAsync("/api/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/products/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_WithoutPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.View]);

        var response = await client.DeleteAsync("/api/products/1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
