# Dataset → Map Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow users to add an uploaded Dataset as a layer on a SavedMap by registering it as a `LayerSource` of a new `Dataset` type.

**Architecture:** Extend `LayerSourceType` with `Dataset = 8`. Add `POST /api/map/sources/from-dataset` that fans out to `IDatasetsContracts.GetByIdAsync`, then creates a normal `LayerSource` with `Url = /api/datasets/{id}/features` and `Metadata = { datasetId }`. Frontend `Edit.tsx` gets a "Add from dataset…" picker. `layer-builders.ts` routes the new type through the existing `GeoJson` rendering branch.

**Tech Stack:** .NET 10 + EF Core (Map module), React 19 + Inertia + MapLibre GL (frontend), xUnit.v3 + FluentAssertions + WebApplicationFactory (tests).

**Spec:** `docs/superpowers/specs/2026-04-08-dataset-map-integration-design.md`

---

## File Structure

**Created:**
- `modules/Map/src/SimpleModule.Map/Endpoints/LayerSources/CreateFromDatasetEndpoint.cs` — POST endpoint
- `modules/Map/src/SimpleModule.Map.Contracts/CreateLayerSourceFromDatasetRequest.cs` — request DTO
- `modules/Map/tests/SimpleModule.Map.Tests/Integration/CreateFromDatasetEndpointTests.cs` — integration tests

**Modified:**
- `modules/Map/src/SimpleModule.Map.Contracts/LayerSourceType.cs` — add `Dataset = 8`
- `modules/Map/src/SimpleModule.Map.Contracts/MapConstants.cs` — add `CreateSourceFromDataset` route
- `modules/Map/src/SimpleModule.Map.Contracts/IMapContracts.cs` — add `CreateLayerSourceFromDatasetAsync`
- `modules/Map/src/SimpleModule.Map/MapService.cs` — implement the new method
- `modules/Map/src/SimpleModule.Map/SimpleModule.Map.csproj` — `ProjectReference` to `SimpleModule.Datasets.Contracts`
- `modules/Map/src/SimpleModule.Map/Pages/lib/layer-builders.ts` — add `Dataset = 8`, route to GeoJson branch
- `modules/Map/src/SimpleModule.Map/Pages/Edit.tsx` — "Add from dataset…" picker
- `modules/Map/src/SimpleModule.Map/Pages/Layers.tsx` — display badge for `Dataset` type

---

## Task 1: Add `Dataset` to `LayerSourceType` enum

**Files:**
- Modify: `modules/Map/src/SimpleModule.Map.Contracts/LayerSourceType.cs`

- [ ] **Step 1: Add the enum value**

```csharp
namespace SimpleModule.Map.Contracts;

public enum LayerSourceType
{
    Wms = 0,
    Wmts = 1,
    Wfs = 2,
    Xyz = 3,
    VectorTile = 4,
    PmTiles = 5,
    Cog = 6,
    GeoJson = 7,
    Dataset = 8,
}
```

- [ ] **Step 2: Build to confirm regen**

Run: `dotnet build modules/Map/src/SimpleModule.Map.Contracts/SimpleModule.Map.Contracts.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add modules/Map/src/SimpleModule.Map.Contracts/LayerSourceType.cs
git commit -m "feat(map): add Dataset to LayerSourceType enum"
```

---

## Task 2: Add Datasets.Contracts project reference to Map

**Files:**
- Modify: `modules/Map/src/SimpleModule.Map/SimpleModule.Map.csproj`

- [ ] **Step 1: Add ProjectReference**

```xml
<Project Sdk="Microsoft.NET.Sdk.StaticWebAssets">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Description>Map module for SimpleModule. MapLibre GL JS-based interactive map with layer source catalog (WMS, WFS, WMTS, PMTiles, COG, XYZ, vector tiles).</Description>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="NetTopologySuite" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\SimpleModule.Map.Contracts\SimpleModule.Map.Contracts.csproj" />
    <ProjectReference Include="..\..\..\Datasets\src\SimpleModule.Datasets.Contracts\SimpleModule.Datasets.Contracts.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Restore + build**

Run: `dotnet build modules/Map/src/SimpleModule.Map/SimpleModule.Map.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add modules/Map/src/SimpleModule.Map/SimpleModule.Map.csproj
git commit -m "chore(map): reference Datasets.Contracts"
```

---

## Task 3: Add request DTO and route constant

**Files:**
- Create: `modules/Map/src/SimpleModule.Map.Contracts/CreateLayerSourceFromDatasetRequest.cs`
- Modify: `modules/Map/src/SimpleModule.Map.Contracts/MapConstants.cs`

- [ ] **Step 1: Create the request DTO**

```csharp
using SimpleModule.Core;

namespace SimpleModule.Map.Contracts;

[Dto]
public class CreateLayerSourceFromDatasetRequest
{
    public Guid DatasetId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
```

- [ ] **Step 2: Add route constant**

In `modules/Map/src/SimpleModule.Map.Contracts/MapConstants.cs`, add inside `Routes`:

```csharp
public const string CreateSourceFromDataset = "/sources/from-dataset";
```

The full `Routes` section becomes:

```csharp
public static class Routes
{
    // Layer source CRUD
    public const string GetAllSources = "/sources";
    public const string GetSourceById = "/sources/{id}";
    public const string CreateSource = "/sources";
    public const string CreateSourceFromDataset = "/sources/from-dataset";
    public const string UpdateSource = "/sources/{id}";
    public const string DeleteSource = "/sources/{id}";

    // Basemap catalog CRUD
    public const string GetAllBasemaps = "/basemaps";
    public const string GetBasemapById = "/basemaps/{id}";
    public const string CreateBasemap = "/basemaps";
    public const string UpdateBasemap = "/basemaps/{id}";
    public const string DeleteBasemap = "/basemaps/{id}";

    // Saved map CRUD
    public const string GetAllMaps = "/maps";
    public const string GetMapById = "/maps/{id}";
    public const string CreateMap = "/maps";
    public const string UpdateMap = "/maps/{id}";
    public const string DeleteMap = "/maps/{id}";

    // Views
    public const string Browse = "/";
    public const string Layers = "/layers";
    public const string Edit = "/{id}/edit";
    public const string View = "/{id}";
}
```

- [ ] **Step 3: Build**

Run: `dotnet build modules/Map/src/SimpleModule.Map.Contracts/SimpleModule.Map.Contracts.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add modules/Map/src/SimpleModule.Map.Contracts/CreateLayerSourceFromDatasetRequest.cs modules/Map/src/SimpleModule.Map.Contracts/MapConstants.cs
git commit -m "feat(map): add CreateLayerSourceFromDatasetRequest contract"
```

---

## Task 4: Extend `IMapContracts` with `CreateLayerSourceFromDatasetAsync`

**Files:**
- Modify: `modules/Map/src/SimpleModule.Map.Contracts/IMapContracts.cs`

- [ ] **Step 1: Read the current interface**

Read `modules/Map/src/SimpleModule.Map.Contracts/IMapContracts.cs` to confirm current shape (do not paste contents — just confirm where to add the method).

- [ ] **Step 2: Add the method**

Add inside `IMapContracts`:

```csharp
Task<LayerSource> CreateLayerSourceFromDatasetAsync(
    CreateLayerSourceFromDatasetRequest request,
    CancellationToken ct = default
);
```

- [ ] **Step 3: Build (will fail in MapService)**

Run: `dotnet build modules/Map/src/SimpleModule.Map/SimpleModule.Map.csproj`
Expected: FAIL with "MapService does not implement interface member CreateLayerSourceFromDatasetAsync". This proves the contract is wired in.

- [ ] **Step 4: Commit**

```bash
git add modules/Map/src/SimpleModule.Map.Contracts/IMapContracts.cs
git commit -m "feat(map): add CreateLayerSourceFromDatasetAsync to contracts"
```

---

## Task 5: Implement `CreateLayerSourceFromDatasetAsync` in `MapService`

**Files:**
- Modify: `modules/Map/src/SimpleModule.Map/MapService.cs`

- [ ] **Step 1: Add `IDatasetsContracts` to constructor**

Change the class declaration:

```csharp
public partial class MapService(
    MapDbContext db,
    SimpleModule.Datasets.Contracts.IDatasetsContracts datasets,
    ILogger<MapService> logger
) : IMapContracts
```

- [ ] **Step 2: Add the method below `CreateLayerSourceAsync`**

Insert in the `// ---------- Layer sources ----------` region, after `CreateLayerSourceAsync`:

```csharp
public async Task<LayerSource> CreateLayerSourceFromDatasetAsync(
    CreateLayerSourceFromDatasetRequest request,
    CancellationToken ct = default
)
{
    var datasetId = SimpleModule.Datasets.Contracts.DatasetId.From(request.DatasetId);
    var dataset =
        await datasets.GetByIdAsync(datasetId, ct)
        ?? throw new NotFoundException("Dataset", request.DatasetId);

    double[]? bounds = dataset.BoundingBox is { } bb
        ? [bb.West, bb.South, bb.East, bb.North]
        : null;

    var source = new LayerSource
    {
        Id = LayerSourceId.From(Guid.NewGuid()),
        Name = string.IsNullOrWhiteSpace(request.Name) ? dataset.Name : request.Name,
        Description = request.Description,
        Type = LayerSourceType.Dataset,
        Url = $"/api/datasets/{request.DatasetId}/features",
        Attribution = null,
        MinZoom = null,
        MaxZoom = null,
        Bounds = bounds,
        Coverage = PolygonFromBounds(bounds),
        Metadata = new Dictionary<string, string>
        {
            ["datasetId"] = request.DatasetId.ToString(),
        },
    };

    db.LayerSources.Add(source);
    await db.SaveChangesAsync(ct);

    LogLayerSourceCreated(logger, source.Id, source.Name);
    return source;
}
```

- [ ] **Step 3: Confirm `BoundingBoxDto` has West/South/East/North**

Run: `grep -n "West\|South\|East\|North" modules/Datasets/src/SimpleModule.Datasets.Contracts/BoundingBoxDto.cs`
Expected: matches for `West`, `South`, `East`, `North` properties. If property names differ (e.g., `MinX/MinY/MaxX/MaxY`), update Step 2 accordingly before continuing.

- [ ] **Step 4: Build**

Run: `dotnet build modules/Map/src/SimpleModule.Map/SimpleModule.Map.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add modules/Map/src/SimpleModule.Map/MapService.cs
git commit -m "feat(map): implement CreateLayerSourceFromDatasetAsync"
```

---

## Task 6: Add `CreateFromDatasetEndpoint`

**Files:**
- Create: `modules/Map/src/SimpleModule.Map/Endpoints/LayerSources/CreateFromDatasetEndpoint.cs`

- [ ] **Step 1: Create the endpoint**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public class CreateFromDatasetEndpoint : IEndpoint
{
    public const string Route = MapConstants.Routes.CreateSourceFromDataset;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                (CreateLayerSourceFromDatasetRequest request, IMapContracts map, CancellationToken ct) =>
                    CrudEndpoints.Create(
                        () => map.CreateLayerSourceFromDatasetAsync(request, ct),
                        s => $"{MapConstants.RoutePrefix}/sources/{s.Id}"
                    )
            )
            .RequirePermission(MapPermissions.ManageSources);
}
```

- [ ] **Step 2: Build the host to confirm endpoint discovery**

Run: `dotnet build template/SimpleModule.Host/SimpleModule.Host.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add modules/Map/src/SimpleModule.Map/Endpoints/LayerSources/CreateFromDatasetEndpoint.cs
git commit -m "feat(map): add POST /sources/from-dataset endpoint"
```

---

## Task 7: Integration test — happy path

**Files:**
- Create: `modules/Map/tests/SimpleModule.Map.Tests/Integration/CreateFromDatasetEndpointTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Map;
using SimpleModule.Map.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Map.Tests.Integration;

[Collection(TestCollections.Integration)]
public class CreateFromDatasetEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    private const string SampleGeoJson = """
        {"type":"FeatureCollection","features":[
            {"type":"Feature","geometry":{"type":"Point","coordinates":[10,20]},"properties":{}}
        ]}
        """;

    [Fact]
    public async Task CreateFromDataset_WithExistingDataset_Returns201_AndLayerSource()
    {
        var client = factory.CreateAuthenticatedClient([
            MapPermissions.ManageSources,
            MapPermissions.ViewSources,
            DatasetsPermissions.Manage,
            DatasetsPermissions.View,
        ]);

        // Upload a dataset first.
        using var content = new MultipartFormDataContent();
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(SampleGeoJson);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/geo+json");
        content.Add(fileContent, "file", "sample.geojson");

        var uploadResponse = await client.PostAsync("/api/datasets/", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var dataset = await uploadResponse.Content.ReadFromJsonAsync<DatasetDto>();
        dataset.Should().NotBeNull();

        // Now create a layer source from it.
        var request = new CreateLayerSourceFromDatasetRequest
        {
            DatasetId = dataset!.Id,
            Name = "Sample dataset layer",
        };

        var response = await client.PostAsJsonAsync("/api/map/sources/from-dataset", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var source = await response.Content.ReadFromJsonAsync<LayerSource>();
        source.Should().NotBeNull();
        source!.Type.Should().Be(LayerSourceType.Dataset);
        source.Name.Should().Be("Sample dataset layer");
        source.Url.Should().Be($"/api/datasets/{dataset.Id}/features");
        source.Metadata.Should().ContainKey("datasetId").WhoseValue.Should().Be(dataset.Id.ToString());
    }

    [Fact]
    public async Task CreateFromDataset_UnknownDataset_Returns404()
    {
        var client = factory.CreateAuthenticatedClient([
            MapPermissions.ManageSources,
        ]);

        var request = new CreateLayerSourceFromDatasetRequest
        {
            DatasetId = Guid.NewGuid(),
        };

        var response = await client.PostAsJsonAsync("/api/map/sources/from-dataset", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateFromDataset_WithoutManagePermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient([MapPermissions.ViewSources]);

        var request = new CreateLayerSourceFromDatasetRequest
        {
            DatasetId = Guid.NewGuid(),
        };

        var response = await client.PostAsJsonAsync("/api/map/sources/from-dataset", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 2: Run the new tests**

Run: `dotnet test modules/Map/tests/SimpleModule.Map.Tests/SimpleModule.Map.Tests.csproj --filter "FullyQualifiedName~CreateFromDatasetEndpointTests"`
Expected: All three tests PASS. If `CreateFromDataset_WithExistingDataset_Returns201_AndLayerSource` fails because the upload route differs, run `dotnet test` against `UploadDatasetEndpoint` first or inspect `modules/Datasets/src/SimpleModule.Datasets/Endpoints/Datasets/UploadDatasetEndpoint.cs` and adjust the multipart shape.

- [ ] **Step 3: Commit**

```bash
git add modules/Map/tests/SimpleModule.Map.Tests/Integration/CreateFromDatasetEndpointTests.cs
git commit -m "test(map): cover CreateFromDataset endpoint"
```

---

## Task 8: Frontend — route `Dataset` through GeoJson builder

**Files:**
- Modify: `modules/Map/src/SimpleModule.Map/Pages/lib/layer-builders.ts`

- [ ] **Step 1: Add `Dataset` to `LayerSourceType` constant**

Change:

```typescript
export const LayerSourceType = {
  Wms: 0,
  Wmts: 1,
  Wfs: 2,
  Xyz: 3,
  VectorTile: 4,
  PmTiles: 5,
  Cog: 6,
  GeoJson: 7,
  Dataset: 8,
} as const;
```

- [ ] **Step 2: Add `Dataset` to the GeoJson switch case**

In `buildMapLibreLayer`, change the existing case:

```typescript
case LayerSourceType.Wfs:
case LayerSourceType.GeoJson:
case LayerSourceType.Dataset: {
  return {
    sourceId,
    source: {
      type: 'geojson',
      data: source.url,
      attribution: source.attribution ?? undefined,
    } as SourceSpecification,
    layer: {
      id: layerId,
      type: 'circle',
      source: sourceId,
      paint: {
        'circle-radius': 4,
        'circle-color': meta.color ?? '#3b82f6',
        'circle-opacity': opacity,
      },
      layout: { visibility },
    } as LayerSpecification,
  };
}
```

- [ ] **Step 3: Lint check**

Run: `npm run check`
Expected: No errors in `layer-builders.ts`.

- [ ] **Step 4: Commit**

```bash
git add modules/Map/src/SimpleModule.Map/Pages/lib/layer-builders.ts
git commit -m "feat(map): render Dataset layer sources via geojson"
```

---

## Task 9: Frontend — "Add from dataset…" picker in `Edit.tsx`

**Files:**
- Modify: `modules/Map/src/SimpleModule.Map/Pages/Edit.tsx`

- [ ] **Step 1: Add state and dataset list state at top of `Edit` component**

Below the existing `useState` declarations (around line 46), add:

```typescript
  const [datasets, setDatasets] = useState<Array<{ id: string; name: string }>>([]);
  const [pickerDatasetId, setPickerDatasetId] = useState<string>('');
  const [datasetsLoaded, setDatasetsLoaded] = useState(false);
  const [availableSources, setAvailableSources] = useState<LayerSource[]>(sources);

  async function loadDatasets() {
    if (datasetsLoaded) return;
    const res = await fetch('/api/datasets/', { headers: { Accept: 'application/json' } });
    if (res.ok) {
      const list = (await res.json()) as Array<{ id: string; name: string }>;
      setDatasets(list);
    }
    setDatasetsLoaded(true);
  }

  async function addFromDataset() {
    if (!pickerDatasetId) return;
    if (layers.length >= maxLayers) return;
    const res = await fetch('/api/map/sources/from-dataset', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ datasetId: pickerDatasetId }),
    });
    if (!res.ok) return;
    const created = (await res.json()) as LayerSource;
    setAvailableSources((prev) => [...prev, created]);
    setLayers([
      ...layers,
      {
        layerSourceId: created.id,
        order: layers.length,
        visible: true,
        opacity: 1,
        styleOverrides: {},
      },
    ]);
    setPickerDatasetId('');
  }
```

- [ ] **Step 2: Replace `sourceById` to use `availableSources`**

Find:

```typescript
  const sourceById = new Map(sources.map((s) => [s.id, s]));
```

Replace with:

```typescript
  const sourceById = new Map(availableSources.map((s) => [s.id, s]));
```

Also pass `availableSources` to `MapCanvas` instead of `sources`:

```typescript
            <MapCanvas
              styleUrl={styleUrl}
              center={[map.centerLng, map.centerLat]}
              zoom={map.zoom}
              pitch={map.pitch}
              bearing={map.bearing}
              layers={layers}
              sources={availableSources}
            />
```

And in the existing source picker `<Select>`, change `sources.map(...)` to `availableSources.map(...)`.

- [ ] **Step 3: Add the dataset picker UI inside the Layers card**

Locate the existing layers picker (`<div className="flex gap-2 pt-2">` containing the source `<Select>`). Directly below it, add a second row:

```tsx
              <div className="flex gap-2 pt-2">
                <Select
                  value={pickerDatasetId}
                  onValueChange={(v) => {
                    setPickerDatasetId(v);
                  }}
                  onOpenChange={(open) => {
                    if (open) loadDatasets();
                  }}
                >
                  <SelectTrigger className="flex-1">
                    <SelectValue placeholder="Add from dataset…" />
                  </SelectTrigger>
                  <SelectContent>
                    {datasets.map((d) => (
                      <SelectItem key={d.id} value={d.id}>
                        {d.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Button
                  onClick={addFromDataset}
                  disabled={!pickerDatasetId || layers.length >= maxLayers}
                >
                  Add
                </Button>
              </div>
```

- [ ] **Step 4: Lint and typecheck**

Run: `npm run check`
Expected: No errors.

- [ ] **Step 5: Build the module**

Run: `npm run build:dev --workspace modules/Map/src/SimpleModule.Map`
Expected: Vite build succeeds. (If the workspace name differs, check `modules/Map/src/SimpleModule.Map/package.json` for the actual name and re-run.)

- [ ] **Step 6: Commit**

```bash
git add modules/Map/src/SimpleModule.Map/Pages/Edit.tsx
git commit -m "feat(map): add 'Add from dataset' picker to map editor"
```

---

## Task 10: Frontend — show `Dataset` badge on Layers catalog page

**Files:**
- Modify: `modules/Map/src/SimpleModule.Map/Pages/Layers.tsx`

- [ ] **Step 1: Read the file to find where source types render**

Read `modules/Map/src/SimpleModule.Map/Pages/Layers.tsx`. Find the table cell or badge that maps `LayerSourceType` numeric values to display strings.

- [ ] **Step 2: Add a label for `Dataset` (value 8)**

If there's a `typeLabels` record or switch, add `8: 'Dataset'` (or `Dataset: 'Dataset'` depending on how the existing entries are keyed). If the file uses inline conditional rendering, add `Dataset` to whatever pattern exists. The added entry must mirror the format of existing entries verbatim — do not introduce a new label style.

If no such mapping exists and the page just shows raw numeric types, skip this task and note in the commit message that the catalog UI does not need a change.

- [ ] **Step 3: Lint**

Run: `npm run check`
Expected: No errors.

- [ ] **Step 4: Commit**

```bash
git add modules/Map/src/SimpleModule.Map/Pages/Layers.tsx
git commit -m "feat(map): label Dataset layer sources in catalog"
```

---

## Task 11: Full build + test sweep

- [ ] **Step 1: Backend build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 2: All tests**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 3: Frontend check**

Run: `npm run check && npm run build:dev`
Expected: All checks pass; all module builds succeed.

- [ ] **Step 4: Validate Inertia pages**

Run: `npm run validate-pages`
Expected: No mismatches.

- [ ] **Step 5: Manual smoke test**

Run: `npm run dev`
- Navigate to `https://localhost:5001/datasets/upload`, upload a small `.geojson` file.
- Wait for status to become Processed.
- Navigate to `https://localhost:5001/map`, open or create a map, click Edit.
- Open "Add from dataset…" — the uploaded dataset should appear.
- Select it, click Add — a new layer should appear in the layer list and render on the map canvas.
- Save the map and reload the view to confirm persistence.

- [ ] **Step 6: Final commit if any fixes were needed**

```bash
git status
# only commit if there are uncommitted fixes from the smoke test
```

---

## Notes for the implementer

- The `IDatasetsContracts` service is already registered by the Datasets module via its own `IModule` `ConfigureServices`. Map only needs the project reference — DI resolves automatically because Map's `IMapContracts → MapService` is constructed by the container.
- `MapService` is `partial`. Logger source-generation methods live at the bottom of the file — do not duplicate or rename them.
- `LayerSourceId` and `DatasetId` are strongly typed Guid wrappers; use `.From(Guid)` to construct, `.Value` (or implicit conversion if defined) to read.
- Frontend type regeneration: the source generator emits `types.ts` for each module on build. After Task 1, rebuilding the host project regenerates `modules/Map/src/SimpleModule.Map/types.ts` so the new enum value is available to TypeScript automatically.
- If the dataset hasn't been processed when added (no bounding box yet), `Bounds` will be null and the layer will still render — MapLibre handles missing bounds gracefully with `geojson` sources.
