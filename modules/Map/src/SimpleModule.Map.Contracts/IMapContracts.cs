namespace SimpleModule.Map.Contracts;

public interface IMapContracts
{
    // Layer source catalog
    Task<IEnumerable<LayerSource>> GetAllLayerSourcesAsync();
    Task<LayerSource?> GetLayerSourceByIdAsync(LayerSourceId id);
    Task<LayerSource> CreateLayerSourceAsync(CreateLayerSourceRequest request);
    Task<LayerSource> CreateLayerSourceFromDatasetAsync(
        CreateLayerSourceFromDatasetRequest request,
        CancellationToken ct = default
    );
    Task<LayerSource> UpdateLayerSourceAsync(LayerSourceId id, UpdateLayerSourceRequest request);
    Task DeleteLayerSourceAsync(LayerSourceId id);

    /// <summary>
    /// Returns the singleton default map. Creates it lazily on first access using
    /// <c>MapModuleOptions</c> defaults so the application always has exactly one map.
    /// </summary>
    Task<SavedMap> GetDefaultMapAsync(CancellationToken ct = default);

    /// <summary>
    /// Upserts the singleton default map. The fixed <see cref="MapConstants.DefaultMapId"/>
    /// is preserved across calls; layers and basemaps are replaced wholesale.
    /// </summary>
    Task<SavedMap> UpdateDefaultMapAsync(
        UpdateDefaultMapRequest request,
        CancellationToken ct = default
    );

    // Basemap catalog
    Task<IEnumerable<Basemap>> GetAllBasemapsAsync();
    Task<Basemap?> GetBasemapByIdAsync(BasemapId id);
    Task<Basemap> CreateBasemapAsync(CreateBasemapRequest request);
    Task<Basemap> UpdateBasemapAsync(BasemapId id, UpdateBasemapRequest request);
    Task DeleteBasemapAsync(BasemapId id);
}
