using System.Net;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace Users.Tests.Integration;

public class ConnectEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly HttpClient _unauthenticatedClient;

    public ConnectEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _unauthenticatedClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            }
        );
    }

    [Fact]
    public async Task GetUserinfo_WithoutValidToken_ReturnsBadRequest()
    {
        // OpenIddict middleware rejects requests without a valid OpenIddict-issued token
        // before the passthrough handler runs — this returns 400, not 401
        var response = await _unauthenticatedClient.GetAsync("/connect/userinfo");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostEndSession_ReturnsSuccess()
    {
        var response = await _unauthenticatedClient.PostAsync("/connect/endsession", null);

        // End session should not return 500
        ((int)response.StatusCode)
            .Should()
            .BeLessThan(500);
    }
}
