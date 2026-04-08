using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.Map.Contracts;

/// <summary>
/// A reusable base map definition. Each basemap points at a MapLibre style JSON URL
/// (vector or raster) and is shown to the user as a swappable background underneath
/// the overlay <see cref="LayerSource"/> stack of a <see cref="SavedMap"/>.
/// </summary>
[Dto]
public class Basemap : AuditableEntity<BasemapId>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>MapLibre style JSON URL (vector or raster style).</summary>
    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client."
    )]
    public string StyleUrl { get; set; } = string.Empty;

    public string? Attribution { get; set; }

    /// <summary>Optional preview thumbnail URL displayed in the basemap switcher.</summary>
    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Stored verbatim and serialized to the JS client."
    )]
    public string? ThumbnailUrl { get; set; }
}
