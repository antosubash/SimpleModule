using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;

namespace SimpleModule.Map.Contracts;

[Dto]
public class CreateLayerSourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LayerSourceType Type { get; set; }

    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client; Uri normalization would change the value."
    )]
    public string Url { get; set; } = string.Empty;

    public string? Attribution { get; set; }
    public int? MinZoom { get; set; }
    public int? MaxZoom { get; set; }

    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Bounding box is a fixed-length [w,s,e,n] tuple serialized as a JSON array."
    )]
    public double[]? Bounds { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = [];
}
