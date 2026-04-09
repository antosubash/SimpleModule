using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;

namespace SimpleModule.Map.Contracts;

/// <summary>
/// Upsert payload for the singleton default map. Carries viewport state, the
/// fallback base style, the ordered overlay layers and the catalog basemaps the
/// switcher exposes. Name/Description are managed server-side.
/// </summary>
[Dto]
public class UpdateDefaultMapRequest
{
    public double CenterLng { get; set; }
    public double CenterLat { get; set; }
    public double Zoom { get; set; }
    public double Pitch { get; set; }
    public double Bearing { get; set; }

    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client; Uri normalization would change the value."
    )]
    public string BaseStyleUrl { get; set; } = string.Empty;

    public List<MapLayer> Layers { get; set; } = [];
    public List<MapBasemap> Basemaps { get; set; } = [];
}
