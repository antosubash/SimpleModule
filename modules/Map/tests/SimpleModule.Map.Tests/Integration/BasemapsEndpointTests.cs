using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Map;
using SimpleModule.Map.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Map.Tests.Integration;

[Collection(TestCollections.Integration)]
public class BasemapsEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    [Fact]
    public async Task GetAllBasemaps_WithViewPermission_ReturnsSeed()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.View]);

        var response = await client.GetAsync("/api/map/basemaps");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var basemaps = await response.Content.ReadFromJsonAsync<List<Basemap>>();
        basemaps.Should().NotBeNull();
        basemaps!
            .Should()
            .Contain(b => b.Name.Contains("Demotiles", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllBasemaps_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/map/basemaps");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBasemap_RoundTrips()
    {
        var client = factory.CreateAuthenticatedClient([
            MapPermissions.View,
            MapPermissions.ManageSources,
        ]);
        var request = new CreateBasemapRequest
        {
            Name = "Custom dark",
            StyleUrl = "https://example.com/dark/style.json",
            Attribution = "© Example",
        };

        var response = await client.PostAsJsonAsync("/api/map/basemaps", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Basemap>();
        created.Should().NotBeNull();

        var get = await client.GetAsync($"/api/map/basemaps/{created!.Id.Value}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateBasemap_WithoutPermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.View]);
        var request = new CreateBasemapRequest
        {
            Name = "Nope",
            StyleUrl = "https://example.com/style.json",
        };

        var response = await client.PostAsJsonAsync("/api/map/basemaps", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateMap_WithBasemaps_PersistsAndRoundTrips()
    {
        var client = factory.CreateAuthenticatedClient([
            MapPermissions.View,
            MapPermissions.Create,
            MapPermissions.ManageSources,
        ]);

        // Use a seeded basemap (Demotiles).
        var basemapsResponse = await client.GetAsync("/api/map/basemaps");
        var basemaps = await basemapsResponse.Content.ReadFromJsonAsync<List<Basemap>>();
        var basemap = basemaps!.First();

        var request = new CreateMapRequest
        {
            Name = "Map with basemap switcher",
            CenterLng = 0,
            CenterLat = 0,
            Zoom = 2,
            BaseStyleUrl = basemap.StyleUrl,
            Basemaps = [new MapBasemap { BasemapId = basemap.Id, Order = 0 }],
        };

        var response = await client.PostAsJsonAsync("/api/map/maps", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<SavedMap>();
        created!.Basemaps.Should().HaveCount(1);
        created.Basemaps[0].BasemapId.Should().Be(basemap.Id);
    }
}
