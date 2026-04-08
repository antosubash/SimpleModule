using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Map;
using SimpleModule.Map.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Map.Tests.Integration;

[Collection(TestCollections.Integration)]
public class MapsEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    [Fact]
    public async Task GetAllMaps_WithViewPermission_Returns200()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.View]);

        var response = await client.GetAsync("/api/map/maps");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMap_WithLayers_PersistsAndRoundTrips()
    {
        var client = factory.CreateAuthenticatedClient([
            MapPermissions.View,
            MapPermissions.Create,
            MapPermissions.ManageSources,
            MapPermissions.ViewSources,
        ]);

        // Seed a layer source so we can reference it.
        var sourceRequest = new CreateLayerSourceRequest
        {
            Name = "Demo XYZ",
            Type = LayerSourceType.Xyz,
            Url = "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
        };
        var sourceResponse = await client.PostAsJsonAsync("/api/map/sources", sourceRequest);
        var source = await sourceResponse.Content.ReadFromJsonAsync<LayerSource>();
        source.Should().NotBeNull();

        var mapRequest = new CreateMapRequest
        {
            Name = "Berlin Overview",
            Description = "Test map composition",
            CenterLng = 13.405,
            CenterLat = 52.52,
            Zoom = 10,
            Pitch = 0,
            Bearing = 0,
            BaseStyleUrl = "https://demotiles.maplibre.org/style.json",
            Layers =
            [
                new MapLayer
                {
                    LayerSourceId = source!.Id,
                    Order = 0,
                    Visible = true,
                    Opacity = 0.8,
                },
            ],
        };

        var response = await client.PostAsJsonAsync("/api/map/maps", mapRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<SavedMap>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Berlin Overview");
        created.Layers.Should().HaveCount(1);
        created.Layers[0].LayerSourceId.Should().Be(source.Id);
        created.Layers[0].Opacity.Should().Be(0.8);

        var get = await client.GetAsync($"/api/map/maps/{created.Id.Value}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await get.Content.ReadFromJsonAsync<SavedMap>();
        fetched!.Layers.Should().HaveCount(1);
        fetched.CenterLng.Should().BeApproximately(13.405, 0.0001);
    }

    [Fact]
    public async Task CreateMap_WithInvalidLatitude_Returns400()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.Create]);
        var request = new CreateMapRequest
        {
            Name = "Bad Map",
            CenterLng = 0,
            CenterLat = 200,
            Zoom = 5,
            BaseStyleUrl = "https://demotiles.maplibre.org/style.json",
        };

        var response = await client.PostAsJsonAsync("/api/map/maps", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateMap_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var request = new CreateMapRequest
        {
            Name = "X",
            BaseStyleUrl = "https://demotiles.maplibre.org/style.json",
        };

        var response = await client.PostAsJsonAsync("/api/map/maps", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMap_NonExistent_Returns404()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.Update]);
        var request = new UpdateMapRequest
        {
            Name = "Nope",
            BaseStyleUrl = "https://demotiles.maplibre.org/style.json",
        };

        var response = await client.PutAsJsonAsync($"/api/map/maps/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
