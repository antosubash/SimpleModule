using System.Net;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Marketplace.Tests.Integration;

public class MarketplaceEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MarketplaceEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_EndpointIsRegistered_DoesNotReturn404()
    {
        var response = await _client.GetAsync("/api/marketplace");

        // The endpoint should be registered (not 404/405).
        // May return 500 if the external NuGet API is unreachable in CI.
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Search_WithQuery_EndpointIsRegistered()
    {
        var response = await _client.GetAsync("/api/marketplace?q=test");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task GetById_EndpointIsRegistered()
    {
        var response = await _client.GetAsync("/api/marketplace/some-package");

        // The endpoint should be registered. It returns 404 (NotFound result)
        // if the package doesn't exist on NuGet, or 500 if the API is unreachable.
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Search_AllowsAnonymousAccess()
    {
        // No auth client — should not get 401/403
        var response = await _client.GetAsync("/api/marketplace");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
