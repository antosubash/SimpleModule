using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Core.Settings;
using SimpleModule.Map;
using SimpleModule.Map.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Map.Tests.Integration;

[Collection(TestCollections.Integration)]
public class MapsEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    [Fact]
    public async Task GetDefaultMap_WithViewPermission_ReturnsSeededMap()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.View]);

        var response = await client.GetAsync("/api/map/default");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var map = await response.Content.ReadFromJsonAsync<SavedMap>();
        map.Should().NotBeNull();
        map!.Id.Should().Be(MapConstants.DefaultMapId);
        map.BaseStyleUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetDefaultMap_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/map/default");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateDefaultMap_WithLayers_PersistsAndRoundTrips()
    {
        var client = factory.CreateAuthenticatedClient([
            MapPermissions.View,
            MapPermissions.Update,
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

        var update = new UpdateDefaultMapRequest
        {
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

        var response = await client.PutAsJsonAsync("/api/map/default", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await response.Content.ReadFromJsonAsync<SavedMap>();
        saved.Should().NotBeNull();
        saved!.Id.Should().Be(MapConstants.DefaultMapId);
        saved.Layers.Should().HaveCount(1);
        saved.Layers[0].LayerSourceId.Should().Be(source.Id);
        saved.Layers[0].Opacity.Should().Be(0.8);

        var get = await client.GetAsync("/api/map/default");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await get.Content.ReadFromJsonAsync<SavedMap>();
        fetched!.Layers.Should().HaveCount(1);
        fetched.CenterLng.Should().BeApproximately(13.405, 0.0001);
    }

    [Fact]
    public async Task UpdateDefaultMap_WithInvalidLatitude_Returns400()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.Update]);
        var request = new UpdateDefaultMapRequest
        {
            CenterLng = 0,
            CenterLat = 200,
            Zoom = 5,
            BaseStyleUrl = "https://demotiles.maplibre.org/style.json",
        };

        var response = await client.PutAsJsonAsync("/api/map/default", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateDefaultMap_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var request = new UpdateDefaultMapRequest
        {
            BaseStyleUrl = "https://demotiles.maplibre.org/style.json",
        };

        var response = await client.PutAsJsonAsync("/api/map/default", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateDefaultMap_WithoutUpdatePermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.View]);
        var request = new UpdateDefaultMapRequest
        {
            BaseStyleUrl = "https://demotiles.maplibre.org/style.json",
        };

        var response = await client.PutAsJsonAsync("/api/map/default", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SettingDefinitions_ExposeMapToolToggles()
    {
        var client = factory.CreateAuthenticatedClient(Array.Empty<string>());

        var response = await client.GetAsync(
            $"/api/settings/definitions?scope={(int)SettingScope.Application}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var definitions = await response.Content.ReadFromJsonAsync<List<SettingDefinition>>();
        definitions.Should().NotBeNull();
        definitions!
            .Should()
            .Contain(d =>
                d.Key == MapConstants.SettingKeys.EnableMeasureTools
                && d.Group == "Map"
                && d.Type == SettingType.Bool
                && d.DefaultValue == "true"
            );
        definitions
            .Should()
            .Contain(d => d.Key == MapConstants.SettingKeys.EnableExportPng && d.Group == "Map");
        definitions
            .Should()
            .Contain(d => d.Key == MapConstants.SettingKeys.EnableGeolocate && d.Group == "Map");
    }
}
