using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SimpleModule.Generator;

internal static class LocationHelper
{
    /// <summary>
    /// Reconstructs a <see cref="Location"/> from a serializable <see cref="SourceLocationRecord"/>.
    /// Returns <see cref="Location.None"/> for null (metadata-only types).
    /// </summary>
    internal static Location ToLocation(SourceLocationRecord? loc)
    {
        if (loc is null)
            return Location.None;

        var start = new LinePosition(loc.Value.StartLine, loc.Value.StartCharacter);
        var end = new LinePosition(loc.Value.EndLine, loc.Value.EndCharacter);
        var span = new LinePositionSpan(start, end);
        return Location.Create(loc.Value.FilePath, TextSpan.FromBounds(0, 0), span);
    }
}
