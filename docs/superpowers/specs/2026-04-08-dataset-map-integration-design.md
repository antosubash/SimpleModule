# Dataset → Map Integration

**Date:** 2026-04-08
**Status:** Approved

## Goal

Allow users to add an uploaded Dataset (Datasets module) as a layer on a SavedMap (Map module), reusing the existing LayerSource catalog rather than introducing a parallel concept.

## Approach

A Dataset becomes a first-class `LayerSource` of a new type `Dataset`. Once registered, it behaves identically to existing sources (ordering, visibility, opacity, basemap composition).

## Backend Changes (Map module)

### 1. Enum
Add to `LayerSourceType`:
```csharp
Dataset = 8,
```

### 2. LayerSource shape for Dataset type
- `Type = LayerSourceType.Dataset`
- `Url = "/api/datasets/{datasetId}/features"` — canonical client fetch endpoint
- `Bounds` — copied from `DatasetDto.BoundingBox` at creation time
- `Metadata` — `{ "datasetId": "<guid>" }`
- `Name` defaults to dataset name; user-overridable via request

### 3. New endpoint
`POST /api/map/layer-sources/from-dataset`

Request:
```json
{ "datasetId": "guid", "name": "string?", "description": "string?" }
```

Behavior:
1. Resolve `IDatasetsContracts.GetByIdAsync(datasetId)`. If null → 404.
2. Build `LayerSource` as described above.
3. Persist via existing `MapService` create path so `Coverage` geometry stays in sync with `Bounds`.
4. Return the created `LayerSource` DTO.

Permission: same as `LayerSources.Create`.

### 4. Project reference
`SimpleModule.Map.csproj` adds `ProjectReference` to `SimpleModule.Datasets.Contracts`. Implementation projects remain isolated per Constitution.

## Frontend Changes (Map module)

### 1. `Pages/Edit.tsx`
In the Layers card, add a second action next to the existing "Add source" picker:

- **"Add from dataset…"** button opens a dataset picker.
- Picker fetches `GET /api/datasets` (existing Datasets endpoint) on demand.
- On select: `POST /api/map/layer-sources/from-dataset` with `{ datasetId }`, then append the returned LayerSource as a new `MapLayer` (same shape as existing add flow).

Reuses existing `sources` prop hydration: the new source is added to local `sources` state so it renders in the layer list immediately without a page reload.

### 2. `Pages/components/MapCanvas.tsx` and `Pages/lib/layer-builders.ts`
Handle `LayerSourceType.Dataset`:
- Fetch `source.url` as GeoJSON (FeatureCollection).
- Add a MapLibre `geojson` source with the fetched data.
- Add default fill/line/circle layers (driven by geometry type), respecting `layer.opacity` and `layer.visible`.

If a `GeoJson` builder already exists, route `Dataset` through the same code path — they're identical at the rendering level.

### 3. `Pages/Layers.tsx`
Display the new type with a badge label (`Dataset`). No new editing UI required — datasets are read-only sources.

### 4. Types
`types.ts` regenerates automatically once the C# enum gains `Dataset = 8`.

## Out of Scope

- Per-dataset MapLibre styling overrides (use existing `styleOverrides` if needed later)
- Auto-refresh of layer data when a dataset is reprocessed
- Vector tile (MVT) serving — datasets stay GeoJSON
- Deleting a dataset does NOT cascade to LayerSources (orphans surface as broken layers; tracked separately if it becomes a problem)

## Tests

**Unit (Map.Tests)**
- `FromDatasetEndpoint` creates LayerSource with name, type, bounds, metadata correctly populated when dataset exists.
- Returns 404 when `IDatasetsContracts.GetByIdAsync` returns null.
- Uses an `IDatasetsContracts` test double — no Datasets module wiring required.

**Integration (Map.Tests, full WebApplicationFactory)**
- Upload dataset → call `from-dataset` → add resulting source to a SavedMap → GET map → confirm the layer is present and references the correct LayerSource.

## File Touch List

- `modules/Map/src/SimpleModule.Map.Contracts/LayerSourceType.cs` — add enum value
- `modules/Map/src/SimpleModule.Map/Endpoints/LayerSources/CreateFromDatasetEndpoint.cs` — new
- `modules/Map/src/SimpleModule.Map/Endpoints/LayerSources/CreateFromDatasetRequest.cs` — new (or inline)
- `modules/Map/src/SimpleModule.Map/MapService.cs` — possibly add helper, or call existing create from endpoint
- `modules/Map/src/SimpleModule.Map/SimpleModule.Map.csproj` — ProjectReference to Datasets.Contracts
- `modules/Map/src/SimpleModule.Map/Pages/Edit.tsx` — dataset picker + handler
- `modules/Map/src/SimpleModule.Map/Pages/Layers.tsx` — badge label
- `modules/Map/src/SimpleModule.Map/Pages/components/MapCanvas.tsx` — Dataset source rendering
- `modules/Map/src/SimpleModule.Map/Pages/lib/layer-builders.ts` — Dataset builder (or shared with GeoJson)
- `modules/Map/tests/SimpleModule.Map.Tests/...` — endpoint + integration tests
