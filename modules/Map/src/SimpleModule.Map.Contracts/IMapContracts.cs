namespace SimpleModule.Map.Contracts;

public interface ILayerSourceContracts
{
    Task<IEnumerable<LayerSource>> GetAllLayerSourcesAsync();
    Task<LayerSource?> GetLayerSourceByIdAsync(LayerSourceId id);
    Task<LayerSource> CreateLayerSourceAsync(CreateLayerSourceRequest request);
    Task<LayerSource> CreateLayerSourceFromDatasetAsync(
        CreateLayerSourceFromDatasetRequest request,
        CancellationToken ct = default
    );
    Task<LayerSource> UpdateLayerSourceAsync(LayerSourceId id, UpdateLayerSourceRequest request);
    Task DeleteLayerSourceAsync(LayerSourceId id);
}

public interface ISavedMapContracts
{
    Task<IEnumerable<SavedMap>> GetAllMapsAsync();
    Task<SavedMap?> GetMapByIdAsync(SavedMapId id);
    Task<SavedMap> CreateMapAsync(CreateMapRequest request);
    Task<SavedMap> UpdateMapAsync(SavedMapId id, UpdateMapRequest request);
    Task DeleteMapAsync(SavedMapId id);
}

public interface IBasemapContracts
{
    Task<IEnumerable<Basemap>> GetAllBasemapsAsync();
    Task<Basemap?> GetBasemapByIdAsync(BasemapId id);
    Task<Basemap> CreateBasemapAsync(CreateBasemapRequest request);
    Task<Basemap> UpdateBasemapAsync(BasemapId id, UpdateBasemapRequest request);
    Task DeleteBasemapAsync(BasemapId id);
}

/// <summary>
/// Aggregate contract for the Map module. Composed of focused sub-interfaces
/// (<see cref="ILayerSourceContracts"/>, <see cref="ISavedMapContracts"/>,
/// <see cref="IBasemapContracts"/>) so consumers can depend on only what they need.
/// </summary>
public interface IMapContracts : ILayerSourceContracts, ISavedMapContracts, IBasemapContracts { }
