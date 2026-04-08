using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;

namespace SimpleModule.Map.Contracts;

[Dto]
public class UpdateMapRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
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
