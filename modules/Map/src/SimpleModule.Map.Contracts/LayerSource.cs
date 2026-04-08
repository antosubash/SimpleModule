using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;
using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.Map.Contracts;

/// <summary>
/// A reusable definition of a remote map data source (WMS, WFS, WMTS, PMTiles, COG, etc.).
/// Layer sources are catalogued centrally and referenced by <see cref="SavedMap"/> compositions.
/// </summary>
[Dto]
public class LayerSource : AuditableEntity<LayerSourceId>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LayerSourceType Type { get; set; }

    /// <summary>Base URL of the source. Format depends on <see cref="Type"/>.</summary>
    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client; Uri normalization would change the value."
    )]
    public string Url { get; set; } = string.Empty;

    public string? Attribution { get; set; }
    public int? MinZoom { get; set; }
    public int? MaxZoom { get; set; }

    /// <summary>
    /// JSON-friendly bounding box [west, south, east, north] in WGS84 (EPSG:4326)
    /// for transport to the React client.
    /// </summary>
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Bounding box is a fixed-length [w,s,e,n] tuple serialized as a JSON array for the client."
    )]
    public double[]? Bounds { get; set; }

    /// <summary>
    /// Server-side spatial coverage polygon backed by a provider-native geometry column
    /// (PostGIS, SQL Server geometry, or SpatiaLite). Used for spatial queries
    /// (e.g., <c>ST_Intersects</c>) but never serialized to the client.
    /// Kept in sync with <see cref="Bounds"/> by <c>MapService</c>.
    /// </summary>
    [JsonIgnore]
    public Geometry? Coverage { get; set; }

    /// <summary>
    /// Free-form, type-specific metadata stored as JSON. Examples:
    /// WMS: { "layers": "OSM-WMS", "format": "image/png", "crs": "EPSG:3857", "transparent": true }
    /// WMTS: { "layer": "...", "tileMatrixSet": "...", "style": "default", "format": "image/png" }
    /// WFS: { "typeName": "...", "outputFormat": "application/json", "version": "2.0.0" }
    /// PMTiles: { "tileType": "vector" }
    /// COG: { "rescale": "0,255", "colormap": "viridis" }
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
