namespace SimpleModule.Map.Contracts;

public interface IMapContracts
{
    // Layer source catalog
    Task<IEnumerable<LayerSource>> GetAllLayerSourcesAsync();
    Task<LayerSource?> GetLayerSourceByIdAsync(LayerSourceId id);
    Task<LayerSource> CreateLayerSourceAsync(CreateLayerSourceRequest request);
    Task<LayerSource> UpdateLayerSourceAsync(LayerSourceId id, UpdateLayerSourceRequest request);
    Task DeleteLayerSourceAsync(LayerSourceId id);

    // Saved maps
    Task<IEnumerable<SavedMap>> GetAllMapsAsync();
    Task<SavedMap?> GetMapByIdAsync(SavedMapId id);
    Task<SavedMap> CreateMapAsync(CreateMapRequest request);
    Task<SavedMap> UpdateMapAsync(SavedMapId id, UpdateMapRequest request);
    Task DeleteMapAsync(SavedMapId id);

    // Basemap catalog
    Task<IEnumerable<Basemap>> GetAllBasemapsAsync();
    Task<Basemap?> GetBasemapByIdAsync(BasemapId id);
    Task<Basemap> CreateBasemapAsync(CreateBasemapRequest request);
    Task<Basemap> UpdateBasemapAsync(BasemapId id, UpdateBasemapRequest request);
    Task DeleteBasemapAsync(BasemapId id);
}
