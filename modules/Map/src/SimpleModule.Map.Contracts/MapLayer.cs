using SimpleModule.Core;

namespace SimpleModule.Map.Contracts;

/// <summary>
/// A reference to a <see cref="LayerSource"/> within a <see cref="SavedMap"/>,
/// with composition-specific overrides (order, visibility, opacity, style).
/// Owned by its parent <see cref="SavedMap"/>.
/// </summary>
[Dto]
public class MapLayer
{
    public LayerSourceId LayerSourceId { get; set; }

    /// <summary>Render order, low values draw first (bottom).</summary>
    public int Order { get; set; }
    public bool Visible { get; set; } = true;

    /// <summary>0..1 opacity multiplier applied client-side.</summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>Optional MapLibre paint/layout property overrides as JSON.</summary>
    public Dictionary<string, string> StyleOverrides { get; set; } = [];
}
