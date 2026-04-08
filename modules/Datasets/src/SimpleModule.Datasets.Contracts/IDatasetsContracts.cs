namespace SimpleModule.Datasets.Contracts;

public interface IDatasetsContracts
{
    Task<IReadOnlyList<DatasetDto>> GetAllAsync(CancellationToken ct = default);

    Task<DatasetDto?> GetByIdAsync(DatasetId id, CancellationToken ct = default);

    Task<DatasetDto> CreateAsync(
        Stream content,
        string fileName,
        string? name,
        CancellationToken ct = default
    );

    Task DeleteAsync(DatasetId id, CancellationToken ct = default);

    Task<Stream?> GetOriginalAsync(DatasetId id, CancellationToken ct = default);

    Task<Stream?> GetDerivativeAsync(
        DatasetId id,
        DatasetFormat format,
        CancellationToken ct = default
    );

    Task<string> GetFeaturesGeoJsonAsync(
        DatasetId id,
        BoundingBoxDto? bbox = null,
        int? limit = null,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<DatasetDto>> FindByBoundingBoxAsync(
        BoundingBoxDto bbox,
        CancellationToken ct = default
    );

    Task EnqueueConversionAsync(
        DatasetId id,
        DatasetFormat? targetFormat = null,
        CancellationToken ct = default
    );
}
