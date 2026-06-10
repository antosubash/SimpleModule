namespace SimpleModule.Cli.Infrastructure;

public readonly record struct ExternalsViolation(string File, string Marker);

/// <summary>
/// Verifies that a module's built frontend bundle externalizes the host-provided
/// libraries (react, react-dom, react/jsx-runtime, @inertiajs/react). A module
/// that inlines its own React copy breaks hooks at runtime (two React instances)
/// — pack fails closed when an inlined-React marker is found.
/// </summary>
public static class BundleExternalsValidator
{
    // Strings that only appear inside React's own source, never in code that
    // imports React as an external.
    private static readonly string[] InlinedReactMarkers =
    [
        "Symbol.for(\"react.element\")",
        "Symbol.for('react.element')",
        "Symbol.for(\"react.transitional.element\")",
        "Symbol.for('react.transitional.element')",
        "react.production.min",
        "react.development",
        "__CLIENT_INTERNALS_DO_NOT_USE",
    ];

    public static IReadOnlyList<ExternalsViolation> Validate(string wwwrootPath)
    {
        var violations = new List<ExternalsViolation>();
        if (!Directory.Exists(wwwrootPath))
        {
            return violations;
        }

        var bundleFiles = Directory
            .EnumerateFiles(wwwrootPath, "*", SearchOption.AllDirectories)
            .Where(f =>
                (
                    f.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase)
                ) && !f.EndsWith(".map", StringComparison.OrdinalIgnoreCase)
            );

        foreach (var file in bundleFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var marker in InlinedReactMarkers)
            {
                if (content.Contains(marker, StringComparison.Ordinal))
                {
                    violations.Add(new ExternalsViolation(file, marker));
                    break;
                }
            }
        }

        return violations;
    }
}
