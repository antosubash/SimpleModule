using SimpleModule.Core.Agents;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Agents;

public sealed class DatasetsToolProvider(IDatasetsContracts datasets) : IAgentToolProvider
{
    [AgentTool(
        Description = "List all GIS datasets with their id, name, format, status, feature count and bounding box."
    )]
    public async Task<IReadOnlyList<DatasetDto>> ListDatasets() => await datasets.GetAllAsync();

    [AgentTool(
        Description = "Get full metadata for a single dataset by id, including extracted CRS, schema, and format-specific metadata."
    )]
    public async Task<DatasetDto?> GetDataset(Guid datasetId) =>
        await datasets.GetByIdAsync(DatasetId.From(datasetId));

    [AgentTool(
        Description = "Query features from a vector dataset, optionally filtered by an EPSG:4326 bounding box. Returns up to `limit` features as GeoJSON."
    )]
    public async Task<string> QueryDatasetFeatures(
        Guid datasetId,
        double? minX = null,
        double? minY = null,
        double? maxX = null,
        double? maxY = null,
        int limit = 100
    )
    {
        BoundingBoxDto? bbox = null;
        if (minX is not null && minY is not null && maxX is not null && maxY is not null)
        {
            bbox = new BoundingBoxDto
            {
                MinX = minX.Value,
                MinY = minY.Value,
                MaxX = maxX.Value,
                MaxY = maxY.Value,
            };
        }
        return await datasets.GetFeaturesGeoJsonAsync(DatasetId.From(datasetId), bbox, limit);
    }

    [AgentTool(
        Description = "Find datasets whose bounding box intersects the given EPSG:4326 bounding box."
    )]
    public async Task<IReadOnlyList<DatasetDto>> FindDatasetsByBoundingBox(
        double minX,
        double minY,
        double maxX,
        double maxY
    ) =>
        await datasets.FindByBoundingBoxAsync(
            new BoundingBoxDto
            {
                MinX = minX,
                MinY = minY,
                MaxX = maxX,
                MaxY = maxY,
            }
        );
}
