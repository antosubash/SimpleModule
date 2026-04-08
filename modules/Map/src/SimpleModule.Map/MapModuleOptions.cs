using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;

namespace SimpleModule.Map;

/// <summary>
/// Configurable options for the Map module.
/// Override defaults from the host application:
/// <code>
/// builder.AddSimpleModule(o =&gt;
/// {
///     o.ConfigureMap(m =&gt; m.BaseStyleUrl = "https://my.tiles/style.json");
/// });
/// </code>
/// </summary>
public class MapModuleOptions : IModuleOptions
{
    /// <summary>Default map center longitude. Default: 0.</summary>
    public double DefaultCenterLng { get; set; }

    /// <summary>Default map center latitude. Default: 20.</summary>
    public double DefaultCenterLat { get; set; } = 20;

    /// <summary>Default zoom level. Default: 2.</summary>
    public double DefaultZoom { get; set; } = 2;

    /// <summary>Default pitch in degrees. Default: 0.</summary>
    public double DefaultPitch { get; set; }

    /// <summary>Default bearing in degrees. Default: 0.</summary>
    public double DefaultBearing { get; set; }

    /// <summary>Base MapLibre style JSON URL used when a saved map has no override.</summary>
    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client; Uri normalization would change the value."
    )]
    public string BaseStyleUrl { get; set; } = "https://demotiles.maplibre.org/style.json";

    /// <summary>Hard cap on layers per saved map. Default: 50.</summary>
    public int MaxLayersPerMap { get; set; } = 50;

    /// <summary>Hard cap on layer source URL length. Default: 2048.</summary>
    public int MaxLayerSourceUrlLength { get; set; } = 2048;

    /// <summary>Enable measure (distance/area) tools in the viewer. Default: true.</summary>
    public bool EnableMeasureTools { get; set; } = true;

    /// <summary>Enable canvas-to-PNG export in the viewer. Default: true.</summary>
    public bool EnableExportPng { get; set; } = true;

    /// <summary>Enable browser geolocation control. Default: true.</summary>
    public bool EnableGeolocate { get; set; } = true;

    /// <summary>
    /// Map spatial geometry columns (<c>Coverage</c>, <c>Center</c>) onto provider-native
    /// spatial types (PostGIS, SQL Server geometry, SpatiaLite). Disable this only when
    /// running against an environment without spatial support, such as in-memory SQLite
    /// integration tests where <c>mod_spatialite</c> is unavailable. Default: true.
    /// </summary>
    public bool EnableSpatialColumns { get; set; } = true;
}
