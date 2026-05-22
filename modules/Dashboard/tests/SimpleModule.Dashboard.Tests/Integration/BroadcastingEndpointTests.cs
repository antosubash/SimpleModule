using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Core.Broadcasting;
using SimpleModule.Core.Inertia;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Dashboard.Tests.Integration;

[Collection(TestCollections.Integration)]
public class BroadcastingEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public BroadcastingEndpointTests(SimpleModuleWebApplicationFactory factory) =>
        _factory = factory;

    private static void AddInertiaHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);
    }

    [Fact]
    public async Task Get_Authenticated_Renders_Channel_For_Current_User()
    {
        const string userId = "user-broadcast-1";
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, userId)
        );
        AddInertiaHeaders(client);

        var response = await client.GetAsync("/broadcasting");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("component").GetString().Should().Be("Dashboard/Broadcasting");

        var props = json.GetProperty("props");
        props.GetProperty("userId").GetString().Should().Be(userId);
        props.GetProperty("channel").GetString().Should().Be(BroadcastChannels.ForUser(userId));
        props.GetProperty("fireUrl").GetString().Should().Be("/api/dashboard/broadcasting/tick");
    }

    [Fact]
    public async Task Tick_Anonymous_Returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/dashboard/broadcasting/tick", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tick_Authenticated_Returns_NoContent()
    {
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, "user-broadcast-2")
        );

        var response = await client.PostAsync("/api/dashboard/broadcasting/tick", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
