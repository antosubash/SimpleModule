using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Core.Inertia;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Dashboard.Tests.Integration;

[Collection(TestCollections.Integration)]
public class HomeEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public HomeEndpointTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    private static void AddInertiaHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);
    }

    [Fact]
    public async Task Anonymous_Get_Renders_With_IsAuthenticated_False_And_Default_Name()
    {
        var client = _factory.CreateClient();
        AddInertiaHeaders(client);

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("component").GetString().Should().Be("Dashboard/Home");

        var props = json.GetProperty("props");
        props.GetProperty("isAuthenticated").GetBoolean().Should().BeFalse();
        props.GetProperty("displayName").GetString().Should().Be("User");
        props.TryGetProperty("isDevelopment", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Authenticated_Get_Renders_With_Claim_Identity_Name()
    {
        var client = _factory.CreateAuthenticatedClient(new Claim(ClaimTypes.Name, "alice"));
        AddInertiaHeaders(client);

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var props = json.GetProperty("props");
        props.GetProperty("isAuthenticated").GetBoolean().Should().BeTrue();
        props.GetProperty("displayName").GetString().Should().Be("alice");
    }
}
