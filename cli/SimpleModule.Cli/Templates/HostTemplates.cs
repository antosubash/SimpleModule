using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Templates;

public sealed class HostTemplates
{
    private const string BaseProjectName = "SimpleModule";

    /// <summary>
    /// Patterns to strip from the template Host .csproj when generating a new project.
    /// Each pattern causes any line containing it to be removed.
    /// </summary>
    private static readonly string[] CsprojStripPatterns =
    [
        "modules",                      // Module ProjectReferences
        "ServiceDefaults",              // Aspire ServiceDefaults
        "InternalsVisibleTo",           // Test assembly visibility
        "NoWarn", "SM0011", "SM0035", "SM0038", // Suppressed analyzer warnings
        "TODO",                         // Dev comments
        "EntityFrameworkCore.Design",   // EF migrations tooling
        "<Import Project",             // MSBuild targets import
        @"framework\",                 // Framework ProjectReferences (replaced by PackageReferences)
        "OutputItemType=",             // Multi-line Generator ref attributes
        "ReferenceOutputAssembly=",
    ];

    private readonly string _templateHostDir;

    public HostTemplates(SolutionContext solution)
    {
        _templateHostDir = Path.Combine(solution.RootPath, "template", "SimpleModule.Host");
    }

    /// <summary>
    /// Transform the Host .csproj: strip module ProjectReferences, ServiceDefaults,
    /// InternalsVisibleTo, NoWarn/SM0011/SM0035/SM0038/TODO lines, EF Core Design package,
    /// and the Import Project line. Replace framework ProjectReferences with PackageReferences.
    /// Replace SimpleModule with projectName.
    /// </summary>
    public string HostCsproj(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "SimpleModule.Host.csproj");
        var lines = File.ReadAllLines(path).ToList();

        // Strip lines containing patterns that should not appear in a new project
        lines.RemoveAll(line =>
            CsprojStripPatterns.Any(p => line.Contains(p, StringComparison.Ordinal))
        );

        // Remove orphaned multi-line XML fragments (e.g., <ProjectReference\n/> after attribute stripping)
        lines.RemoveAll(line =>
        {
            var trimmed = line.TrimStart();
            return trimmed == "/>"
                || (trimmed.StartsWith("<ProjectReference", StringComparison.Ordinal) && !trimmed.Contains("Include=", StringComparison.Ordinal));
        });

        // Remove empty PropertyGroup and ItemGroup elements
        lines = RemoveEmptyXmlBlocks(lines, "PropertyGroup");
        lines = RemoveEmptyXmlBlocks(lines, "ItemGroup");

        lines = TemplateExtractor.CollapseBlankLines(lines);

        var content = string.Join(Environment.NewLine, lines);

        content = ReplaceProjectName(content, projectName);

        // Insert framework PackageReferences before the closing </Project> tag
        var packageRefs = """
              <ItemGroup>
                <PackageReference Include="SimpleModule.Hosting" />
                <PackageReference Include="SimpleModule.Generator" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all" />
              </ItemGroup>
            """;

        content = content.Replace(
            "</Project>",
            packageRefs + Environment.NewLine + "</Project>",
            StringComparison.Ordinal
        );

        return content;
    }

    /// <summary>
    /// Copy Program.cs, remove ServiceDefaults and MapDefaultEndpoints lines.
    /// Framework method names (AddSimpleModule, UseSimpleModule) are kept as-is.
    /// </summary>
    public string ProgramCs()
    {
        var path = Path.Combine(_templateHostDir, "Program.cs");
        var lines = File.ReadAllLines(path).ToList();

        lines.RemoveAll(line =>
            line.Contains("ServiceDefaults", StringComparison.Ordinal)
            || line.Contains("MapDefaultEndpoints", StringComparison.Ordinal)
        );

        lines = TemplateExtractor.CollapseBlankLines(lines);

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Copy App.razor, replace SimpleModule with projectName.
    /// </summary>
    public string AppRazor(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Components", "App.razor");
        var content = File.ReadAllText(path);
        return ReplaceProjectName(content, projectName);
    }

    /// <summary>
    /// Copy InertiaShell.razor, replace SimpleModule with projectName.
    /// </summary>
    public string InertiaShellRazor(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Components", "InertiaShell.razor");
        var content = File.ReadAllText(path);
        return ReplaceProjectName(content, projectName);
    }

    /// <summary>
    /// Generate a clean Routes.razor WITHOUT the AdditionalAssemblies Users module reference.
    /// </summary>
    public static string RoutesRazor()
    {
        return """
            <Router AppAssembly="typeof(App).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="routeData" DefaultLayout="typeof(SimpleModule.Blazor.Components.Layout.MainLayout)" />
                </Found>
            </Router>
            """;
    }

    /// <summary>
    /// Copy _Imports.razor, replace SimpleModule with projectName.
    /// </summary>
    public string ImportsRazor(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Components", "_Imports.razor");
        var content = File.ReadAllText(path);
        return ReplaceProjectName(content, projectName);
    }

    /// <summary>
    /// Copy app.tsx as-is.
    /// </summary>
    public string AppTsx()
    {
        var path = Path.Combine(_templateHostDir, "ClientApp", "app.tsx");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Copy vite.config.ts as-is.
    /// </summary>
    public string ViteConfig()
    {
        var path = Path.Combine(_templateHostDir, "ClientApp", "vite.config.ts");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Copy validate-pages.mjs as-is.
    /// </summary>
    public string ValidatePages()
    {
        var path = Path.Combine(_templateHostDir, "ClientApp", "validate-pages.mjs");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Copy ClientApp/package.json, replace @simplemodule/app with @{projectName}/app.
    /// </summary>
    public string ClientAppPackageJson(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "ClientApp", "package.json");
        var content = File.ReadAllText(path);
        return content.Replace(
            "@simplemodule/app",
            $"@{projectName.ToLowerInvariant()}/app",
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Return a clean Styles/app.css with tailwindcss import, theme import,
    /// and @source directives for modules. NO _scan/ import.
    /// </summary>
    public static string AppCss()
    {
        return """
            @import "tailwindcss";
            @import "@simplemodule/theme-default/theme.css";
            @source "../../../modules/**/Components/**/*.razor";
            @source "../../../modules/**/Views/**/*.tsx";
            @source "../../../modules/**/Pages/**/*.tsx";
            """;
    }

    /// <summary>
    /// Copy appsettings.json as-is.
    /// </summary>
    public string AppSettings()
    {
        var path = Path.Combine(_templateHostDir, "appsettings.json");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Copy appsettings.Development.json as-is.
    /// </summary>
    public string AppSettingsDevelopment()
    {
        var path = Path.Combine(_templateHostDir, "appsettings.Development.json");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Copy launchSettings.json, replace SimpleModule with projectName.
    /// </summary>
    public string LaunchSettings(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Properties", "launchSettings.json");
        var content = File.ReadAllText(path);
        return ReplaceProjectName(content, projectName);
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces "SimpleModule" with projectName in content, preserving framework names
    /// (SimpleModule.Hosting, SimpleModule.Generator, SimpleModule.Blazor, SimpleModule.Core,
    /// SimpleModule.Database, SimpleModule.DevTools).
    /// </summary>
    private static string ReplaceProjectName(string content, string projectName)
    {
        // Framework package/project names that must NOT be renamed
        var frameworkNames = new[]
        {
            "SimpleModule.Hosting",
            "SimpleModule.Generator",
            "SimpleModule.Blazor",
            "SimpleModule.Core",
            "SimpleModule.Database",
            "SimpleModule.DevTools",
        };

        // Protect framework names with placeholders
        var placeholders = new (string Original, string Placeholder)[frameworkNames.Length];
        for (var i = 0; i < frameworkNames.Length; i++)
        {
            placeholders[i] = (frameworkNames[i], $"%%FW_{i}%%");
            content = content.Replace(frameworkNames[i], placeholders[i].Placeholder, StringComparison.Ordinal);
        }

        // Replace the project name
        content = content.Replace(BaseProjectName, projectName, StringComparison.Ordinal);

        // Restore framework names
        for (var i = 0; i < placeholders.Length; i++)
        {
            content = content.Replace(placeholders[i].Placeholder, placeholders[i].Original, StringComparison.Ordinal);
        }

        return content;
    }

    /// <summary>
    /// Removes XML element blocks (e.g., PropertyGroup, ItemGroup) that are empty
    /// (contain only whitespace between opening and closing tags).
    /// </summary>
    private static List<string> RemoveEmptyXmlBlocks(List<string> lines, string elementName)
    {
        var result = new List<string>();
        var openTag = $"<{elementName}";
        var closeTag = $"</{elementName}>";

        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();

            if (trimmed.StartsWith(openTag, StringComparison.Ordinal))
            {
                // Look ahead to see if the block is empty
                var blockLines = new List<string> { lines[i] };
                var j = i + 1;
                var isEmpty = true;

                while (j < lines.Count)
                {
                    var innerTrimmed = lines[j].TrimStart();
                    blockLines.Add(lines[j]);

                    if (innerTrimmed.StartsWith(closeTag, StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (!string.IsNullOrWhiteSpace(lines[j]))
                    {
                        isEmpty = false;
                    }

                    j++;
                }

                if (isEmpty && j < lines.Count)
                {
                    // Skip the entire empty block
                    i = j;
                    continue;
                }

                // Not empty, add all lines normally
                result.Add(lines[i]);
                continue;
            }

            result.Add(lines[i]);
        }

        return result;
    }
}
