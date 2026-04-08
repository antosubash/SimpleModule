using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Map;
using SimpleModule.Map.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Map.Tests.Integration;

[Collection(TestCollections.Integration)]
public class CreateFromDatasetEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    private static readonly Guid KnownDatasetId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111"
    );

    private HttpClient CreateClientWithFakeDatasets(params string[] permissions)
    {
        // Force the base factory to initialize the shared in-memory DB schema
        // (EnsureDatabasesInitialized is private; CreateAuthenticatedClient triggers it).
        factory.CreateAuthenticatedClient(Array.Empty<string>()).Dispose();

        var host = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDatasetsContracts>();
                services.AddScoped<IDatasetsContracts>(_ => new FakeDatasetsContracts());
            });
        });

        var client = host.CreateClient();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user-id") };
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }
        var header = string.Join(";", claims.Select(c => $"{c.Type}={c.Value}"));
        client.DefaultRequestHeaders.Add("X-Test-Claims", header);
        return client;
    }

    [Fact]
    public async Task CreateFromDataset_WithExistingDataset_Returns201_AndLayerSource()
    {
        using var client = CreateClientWithFakeDatasets(
            MapPermissions.ManageSources,
            MapPermissions.ViewSources
        );

        var request = new CreateLayerSourceFromDatasetRequest
        {
            DatasetId = KnownDatasetId,
            Name = "Sample dataset layer",
        };

        var response = await client.PostAsJsonAsync("/api/map/sources/from-dataset", request);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        var source = await response.Content.ReadFromJsonAsync<LayerSource>();
        source.Should().NotBeNull();
        source!.Type.Should().Be(LayerSourceType.Dataset);
        source.Name.Should().Be("Sample dataset layer");
        source.Url.Should().Be($"/api/datasets/{KnownDatasetId}/features");
        source
            .Metadata.Should()
            .ContainKey("datasetId")
            .WhoseValue.Should()
            .Be(KnownDatasetId.ToString());
        source.Bounds.Should().Equal(-10, -20, 30, 40);
    }

    [Fact]
    public async Task CreateFromDataset_DefaultsNameFromDataset_WhenNameOmitted()
    {
        using var client = CreateClientWithFakeDatasets(
            MapPermissions.ManageSources,
            MapPermissions.ViewSources
        );

        var request = new CreateLayerSourceFromDatasetRequest { DatasetId = KnownDatasetId };

        var response = await client.PostAsJsonAsync("/api/map/sources/from-dataset", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var source = await response.Content.ReadFromJsonAsync<LayerSource>();
        source!.Name.Should().Be(FakeDatasetsContracts.DatasetName);
    }

    [Fact]
    public async Task CreateFromDataset_UnknownDataset_Returns404()
    {
        using var client = CreateClientWithFakeDatasets(MapPermissions.ManageSources);

        var request = new CreateLayerSourceFromDatasetRequest { DatasetId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync("/api/map/sources/from-dataset", request);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, because: body);
    }

    [Fact]
    public async Task CreateFromDataset_WithoutManagePermission_Returns403()
    {
        using var client = CreateClientWithFakeDatasets(MapPermissions.ViewSources);

        var request = new CreateLayerSourceFromDatasetRequest { DatasetId = KnownDatasetId };

        var response = await client.PostAsJsonAsync("/api/map/sources/from-dataset", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class FakeDatasetsContracts : IDatasetsContracts
    {
        public const string DatasetName = "Fake dataset";

        public Task<IReadOnlyList<DatasetDto>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DatasetDto>>([]);

        public Task<DatasetDto?> GetByIdAsync(DatasetId id, CancellationToken ct = default)
        {
            if (id.Value == KnownDatasetId)
            {
                return Task.FromResult<DatasetDto?>(
                    new DatasetDto
                    {
                        Id = KnownDatasetId,
                        Name = DatasetName,
                        OriginalFileName = "fake.geojson",
                        Format = DatasetFormat.GeoJson,
                        Status = DatasetStatus.Ready,
                        BoundingBox = new BoundingBoxDto
                        {
                            MinX = -10,
                            MinY = -20,
                            MaxX = 30,
                            MaxY = 40,
                        },
                    }
                );
            }

            return Task.FromResult<DatasetDto?>(null);
        }

        public Task<DatasetDto> CreateAsync(
            Stream content,
            string fileName,
            string? name,
            CancellationToken ct = default
        ) => throw new NotSupportedException();

        public Task DeleteAsync(DatasetId id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Stream?> GetOriginalAsync(DatasetId id, CancellationToken ct = default) =>
            Task.FromResult<Stream?>(null);

        public Task<Stream?> GetDerivativeAsync(
            DatasetId id,
            DatasetFormat format,
            CancellationToken ct = default
        ) => Task.FromResult<Stream?>(null);

        public Task<string> GetFeaturesGeoJsonAsync(
            DatasetId id,
            BoundingBoxDto? bbox = null,
            int? limit = null,
            CancellationToken ct = default
        ) => Task.FromResult("{}");

        public Task<IReadOnlyList<DatasetDto>> FindByBoundingBoxAsync(
            BoundingBoxDto bbox,
            CancellationToken ct = default
        ) => Task.FromResult<IReadOnlyList<DatasetDto>>([]);

        public Task EnqueueConversionAsync(
            DatasetId id,
            DatasetFormat? targetFormat = null,
            CancellationToken ct = default
        ) => Task.CompletedTask;
    }
}
