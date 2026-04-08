using System.Text.Json;

namespace SimpleModule.Datasets.Processing;

internal static class GeoJsonBboxWalker
{
    public static void Expand(
        JsonElement element,
        ref double minX,
        ref double minY,
        ref double maxX,
        ref double maxY
    )
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        using var enumerator = element.EnumerateArray().GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }
        if (enumerator.Current.ValueKind == JsonValueKind.Number)
        {
            var x = enumerator.Current.GetDouble();
            if (!enumerator.MoveNext())
            {
                return;
            }
            var y = enumerator.Current.GetDouble();
            if (x < minX)
            {
                minX = x;
            }
            if (y < minY)
            {
                minY = y;
            }
            if (x > maxX)
            {
                maxX = x;
            }
            if (y > maxY)
            {
                maxY = y;
            }
            return;
        }
        foreach (var child in element.EnumerateArray())
        {
            Expand(child, ref minX, ref minY, ref maxX, ref maxY);
        }
    }
}
