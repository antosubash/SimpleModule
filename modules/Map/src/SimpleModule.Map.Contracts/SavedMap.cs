using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;
using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.Map.Contracts;

/// <summary>
/// A named, persistent map composition: viewport state, base style and an ordered list of
/// <see cref="MapLayer"/> entries pointing at catalogued <see cref="LayerSource"/> definitions.
/// </summary>
[Dto]
public class SavedMap : AuditableEntity<SavedMapId>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>JSON-friendly viewport center longitude (WGS84) for the React client.</summary>
    public double CenterLng { get; set; }

    /// <summary>JSON-friendly viewport center latitude (WGS84) for the React client.</summary>
    public double CenterLat { get; set; }

    public double Zoom { get; set; }
    public double Pitch { get; set; }
    public double Bearing { get; set; }

    /// <summary>MapLibre style JSON URL used as the base layer for this map.</summary>
    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client; Uri normalization would change the value."
    )]
    public string BaseStyleUrl { get; set; } = string.Empty;

    /// <summary>
    /// Server-side spatial point of the viewport center, backed by a provider-native
    /// geometry column. Used for spatial queries (e.g., "maps near this location").
    /// Kept in sync with <see cref="CenterLng"/>/<see cref="CenterLat"/> by <c>MapService</c>.
    /// </summary>
    [JsonIgnore]
    public Point? Center { get; set; }

    public List<MapLayer> Layers { get; set; } = [];

    /// <summary>
    /// Catalogued basemaps available in this map's basemap switcher. The entry with
    /// the lowest <see cref="MapBasemap.Order"/> is the default. When empty the map
    /// falls back to <see cref="BaseStyleUrl"/>.
    /// </summary>
    public List<MapBasemap> Basemaps { get; set; } = [];
}
