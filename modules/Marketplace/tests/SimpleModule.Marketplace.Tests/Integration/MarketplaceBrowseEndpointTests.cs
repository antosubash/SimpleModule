using System.Net;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Marketplace.Tests.Integration;

public class MarketplaceBrowseEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MarketplaceBrowseEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Browse_EndpointIsRegistered_DoesNotReturn404()
    {
        var response = await _client.GetAsync("/marketplace/browse");

        // The endpoint should be registered (not 404/405).
        // It may return 500 if the external NuGet API is unreachable in CI,
        // but it should never be a 404 (route not found).
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
    }
}
