namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Orders dotted version strings numerically (1.10.0 &gt; 1.9.0) with releases
/// above their prereleases (1.0.0 &gt; 1.0.0-rc). The single source of version
/// ordering for the CLI — local feed selection, registry version picking and
/// global-cache lookups all share it.
/// </summary>
public sealed class SemVerStringComparer : IComparer<string>
{
    public static readonly SemVerStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        var (xCore, xPre) = Split(x ?? "");
        var (yCore, yPre) = Split(y ?? "");

        var xParts = xCore.Split('.');
        var yParts = yCore.Split('.');
        for (var i = 0; i < Math.Max(xParts.Length, yParts.Length); i++)
        {
            var xNum = i < xParts.Length && int.TryParse(xParts[i], out var xv) ? xv : 0;
            var yNum = i < yParts.Length && int.TryParse(yParts[i], out var yv) ? yv : 0;
            var byNum = xNum.CompareTo(yNum);
            if (byNum != 0)
            {
                return byNum;
            }
        }

        if (xPre.Length == 0 && yPre.Length == 0)
        {
            return 0;
        }

        if (xPre.Length == 0)
        {
            return 1;
        }

        if (yPre.Length == 0)
        {
            return -1;
        }

        return string.CompareOrdinal(xPre, yPre);
    }

    public static bool IsPrerelease(string version) =>
        version.Contains('-', StringComparison.Ordinal);

    private static (string Core, string Prerelease) Split(string version)
    {
        var dash = version.IndexOf('-', StringComparison.Ordinal);
        return dash < 0 ? (version, "") : (version[..dash], version[(dash + 1)..]);
    }
}
