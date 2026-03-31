using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Core.Inertia;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Core.Tests.Inertia;

public class InertiaResultTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public InertiaResultTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InertiaEndpoint_WithoutInertiaHeader_ReturnsHtml()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/products/browse");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("id=\"app\"");
        html.Should().Contain("<script data-page=\"app\" type=\"application/json\">");
        html.Should().Contain("/js/app.js");
    }

    [Fact]
    public async Task InertiaEndpoint_WithInertiaHeader_ReturnsJson()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await client.GetAsync("/products/browse");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("component").GetString().Should().Be("Products/Browse");
        json.TryGetProperty("props", out _).Should().BeTrue();
        json.TryGetProperty("url", out _).Should().BeTrue();
        json.TryGetProperty("version", out _).Should().BeTrue();
    }

    [Fact]
    public async Task InertiaEndpoint_JsonResponse_ContainsCorrectUrl()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await client.GetAsync("/products/browse");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("url").GetString().Should().Be("/products/browse");
    }

    [Fact]
    public async Task InertiaEndpoint_JsonResponse_HasInertiaResponseHeader()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await client.GetAsync("/products/browse");
        response.Headers.Contains("X-Inertia").Should().BeTrue();
        response.Headers.GetValues("X-Inertia").Should().Contain("true");
    }

    [Fact]
    public async Task InertiaEndpoint_HtmlResponse_ContainsImportMap()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/products/browse");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("importmap");
        html.Should().Contain("react");
    }

    [Fact]
    public async Task InertiaMiddleware_AddsVersionHeader()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/products/browse");
        response.Headers.Contains("X-Inertia-Version").Should().BeTrue();
    }

    [Fact]
    public async Task InertiaMiddleware_VersionMismatch_Returns409()
    {
        // Create a handler that doesn't auto-redirect
        using var handler = _factory.Server.CreateHandler();
        using var client = new HttpClient(handler) { BaseAddress = _factory.Server.BaseAddress };
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "wrong-version");

        var response = await client.GetAsync("/products/browse");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
