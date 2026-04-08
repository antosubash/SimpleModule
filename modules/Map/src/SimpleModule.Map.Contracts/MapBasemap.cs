using SimpleModule.Core;

namespace SimpleModule.Map.Contracts;

/// <summary>
/// A reference to a catalogued <see cref="Basemap"/> from a <see cref="SavedMap"/>.
/// The viewer's basemap switcher offers all entries in this list; the lowest
/// <see cref="Order"/> is shown by default.
/// </summary>
[Dto]
public class MapBasemap
{
    public BasemapId BasemapId { get; set; }

    /// <summary>Display order in the basemap switcher; lowest is the default.</summary>
    public int Order { get; set; }
}
