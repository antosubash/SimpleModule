using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Map;
using SimpleModule.Map.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Map.Tests.Integration;

[Collection(TestCollections.Integration)]
public class LayerSourcesEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    [Fact]
    public async Task GetAllSources_WithViewPermission_Returns200()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.ViewSources]);

        var response = await client.GetAsync("/api/map/sources");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllSources_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/map/sources");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllSources_WithoutPermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.View]);

        var response = await client.GetAsync("/api/map/sources");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateSource_WithManagePermission_Returns201_AndRoundTrips()
    {
        var client = factory.CreateAuthenticatedClient([
            MapPermissions.ManageSources,
            MapPermissions.ViewSources,
        ]);
        var request = new CreateLayerSourceRequest
        {
            Name = "Terrestris OSM-WMS",
            Type = LayerSourceType.Wms,
            Url = "https://ows.terrestris.de/osm/service?",
            Attribution = "© OpenStreetMap contributors",
            Bounds = [-180, -85, 180, 85],
            Metadata = new Dictionary<string, string>
            {
                ["layers"] = "OSM-WMS",
                ["format"] = "image/png",
                ["crs"] = "EPSG:3857",
            },
        };

        var response = await client.PostAsJsonAsync("/api/map/sources", request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var created = await response.Content.ReadFromJsonAsync<LayerSource>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Terrestris OSM-WMS");
        created.Type.Should().Be(LayerSourceType.Wms);
        created.Metadata.Should().ContainKey("layers");

        var get = await client.GetAsync($"/api/map/sources/{created.Id.Value}");
        get.StatusCode.Should().Be(HttpStatusCode.OK, $"id={created.Id.Value} body={body}");
        var fetched = await get.Content.ReadFromJsonAsync<LayerSource>();
        fetched!.Url.Should().Be(request.Url);
    }

    [Fact]
    public async Task CreateSource_WithoutManagePermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.ViewSources]);
        var request = new CreateLayerSourceRequest
        {
            Name = "Test",
            Type = LayerSourceType.Xyz,
            Url = "https://example.com/{z}/{x}/{y}.png",
        };

        var response = await client.PostAsJsonAsync("/api/map/sources", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateSource_WithBlankName_Returns400()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.ManageSources]);
        var request = new CreateLayerSourceRequest
        {
            Name = "",
            Type = LayerSourceType.Wms,
            Url = "https://example.com/wms",
        };

        var response = await client.PostAsJsonAsync("/api/map/sources", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSource_NonExistent_Returns404()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.ManageSources]);
        var request = new UpdateLayerSourceRequest
        {
            Name = "Nope",
            Type = LayerSourceType.Wms,
            Url = "https://example.com/wms",
        };

        var response = await client.PutAsJsonAsync($"/api/map/sources/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSource_RoundTrip_Returns204()
    {
        var client = factory.CreateAuthenticatedClient([
            MapPermissions.ManageSources,
            MapPermissions.ViewSources,
        ]);
        var request = new CreateLayerSourceRequest
        {
            Name = "Throwaway",
            Type = LayerSourceType.PmTiles,
            Url = "https://example.com/tiles.pmtiles",
        };
        var createResponse = await client.PostAsJsonAsync("/api/map/sources", request);
        var created = await createResponse.Content.ReadFromJsonAsync<LayerSource>();

        var response = await client.DeleteAsync($"/api/map/sources/{created!.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
