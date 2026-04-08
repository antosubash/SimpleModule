using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;

namespace SimpleModule.Map.Contracts;

[Dto]
public class UpdateBasemapRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client."
    )]
    public string StyleUrl { get; set; } = string.Empty;

    public string? Attribution { get; set; }

    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client."
    )]
    public string? ThumbnailUrl { get; set; }
}
