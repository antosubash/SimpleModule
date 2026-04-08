namespace SimpleModule.Map.Contracts;

/// <summary>
/// Supported layer source types for the map. Each value maps to a specific
/// MapLibre source/protocol on the client.
/// </summary>
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
