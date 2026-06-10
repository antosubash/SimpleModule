using System.Globalization;

namespace SimpleModule.Cli.Infrastructure;

public readonly record struct CompatResult(bool Compatible, string Reason);

/// <summary>
/// Evaluates a module manifest's <c>frameworkCompat</c> SemVer range against the
/// host's SimpleModule.Core version. Supported range grammar (what the source
/// generator emits): <c>&gt;=X.Y.Z[-pre]</c> optionally followed by <c>&lt;A.B.C[-pre]</c>.
/// </summary>
public static class FrameworkCompatChecker
{
    public static CompatResult Check(string range, string hostVersion)
    {
        if (string.IsNullOrWhiteSpace(range))
        {
            return new CompatResult(
                true,
                "Module declares no framework compatibility range; assuming compatible."
            );
        }

        if (!SemVer.TryParse(hostVersion, out var host))
        {
            return new CompatResult(
                false,
                $"Host framework version '{hostVersion}' is not a valid semantic version."
            );
        }

        SemVer? lower = null;
        SemVer? upper = null;
        foreach (var part in range.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith(">=", StringComparison.Ordinal))
            {
                if (!SemVer.TryParse(part[2..], out var parsed))
                {
                    return Unparseable(range);
                }

                lower = parsed;
            }
            else if (part.StartsWith('<'))
            {
                if (!SemVer.TryParse(part[1..], out var parsed))
                {
                    return Unparseable(range);
                }

                upper = parsed;
            }
            else
            {
                return Unparseable(range);
            }
        }

        if (lower is null && upper is null)
        {
            return Unparseable(range);
        }

        if (lower is not null && host.CompareTo(lower.Value) < 0)
        {
            return new CompatResult(
                false,
                $"Host framework {hostVersion} is older than the module's minimum {lower}."
            );
        }

        if (upper is not null && host.CompareTo(upper.Value) >= 0)
        {
            return new CompatResult(
                false,
                $"Host framework {hostVersion} is at or above the module's exclusive upper bound {upper}."
            );
        }

        return new CompatResult(true, $"Host framework {hostVersion} satisfies '{range}'.");
    }

    private static CompatResult Unparseable(string range) =>
        new(false, $"Could not parse framework compatibility range '{range}'.");

    private readonly record struct SemVer(int Major, int Minor, int Patch, string Prerelease)
        : IComparable<SemVer>
    {
        public static bool TryParse(string input, out SemVer version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var core = input;
            var prerelease = "";
            var dash = input.IndexOf('-', StringComparison.Ordinal);
            if (dash >= 0)
            {
                core = input[..dash];
                prerelease = input[(dash + 1)..];
            }

            var parts = core.Split('.');
            if (parts.Length is < 2 or > 3)
            {
                return false;
            }

            if (
                !int.TryParse(
                    parts[0],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var major
                )
                || !int.TryParse(
                    parts[1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var minor
                )
            )
            {
                return false;
            }

            var patch = 0;
            if (
                parts.Length == 3
                && !int.TryParse(
                    parts[2],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out patch
                )
            )
            {
                return false;
            }

            version = new SemVer(major, minor, patch, prerelease);
            return true;
        }

        public int CompareTo(SemVer other)
        {
            var byMajor = Major.CompareTo(other.Major);
            if (byMajor != 0)
            {
                return byMajor;
            }

            var byMinor = Minor.CompareTo(other.Minor);
            if (byMinor != 0)
            {
                return byMinor;
            }

            var byPatch = Patch.CompareTo(other.Patch);
            if (byPatch != 0)
            {
                return byPatch;
            }

            // SemVer: a prerelease sorts BELOW its release (1.0.0-x < 1.0.0).
            if (Prerelease.Length == 0 && other.Prerelease.Length == 0)
            {
                return 0;
            }

            if (Prerelease.Length == 0)
            {
                return 1;
            }

            if (other.Prerelease.Length == 0)
            {
                return -1;
            }

            return string.CompareOrdinal(Prerelease, other.Prerelease);
        }

        public override string ToString() =>
            Prerelease.Length == 0
                ? $"{Major}.{Minor}.{Patch}"
                : $"{Major}.{Minor}.{Patch}-{Prerelease}";
    }
}
