using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Core.Inertia;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Products.Tests.Integration;

[Collection("Integration")]
public class ProductsBrowseEndpointTests
{
    private readonly HttpClient _client;

    public ProductsBrowseEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Browse_ReturnsHtmlPage()
    {
        var response = await _client.GetAsync("/products/browse");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Browse_WithInertia_ReturnsProductsData()
    {
        _client.DefaultRequestHeaders.Add("X-Inertia", "true");
        _client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await _client.GetAsync("/products/browse");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("component").GetString().Should().Be("Products/Browse");

        var props = json.GetProperty("props");
        props.TryGetProperty("products", out var products).Should().BeTrue();
        products.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Browse_ProductsHaveExpectedFields()
    {
        _client.DefaultRequestHeaders.Add("X-Inertia", "true");
        _client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await _client.GetAsync("/products/browse");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var firstProduct = json.GetProperty("props").GetProperty("products")[0];

        firstProduct.TryGetProperty("id", out _).Should().BeTrue();
        firstProduct.TryGetProperty("name", out _).Should().BeTrue();
        firstProduct.TryGetProperty("price", out _).Should().BeTrue();
    }
}
