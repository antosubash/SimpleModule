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
        "modules", // Module ProjectReferences
        "ServiceDefaults", // Aspire ServiceDefaults
        "InternalsVisibleTo", // Test assembly visibility
        "NoWarn",
        "SM0011",
        "SM0035",
        "SM0038", // Suppressed analyzer warnings
        "TODO", // Dev comments
        "EntityFrameworkCore.Design", // EF migrations tooling
        "<Import Project", // MSBuild targets import
        @"framework\", // Framework ProjectReferences (replaced by PackageReferences)
        "OutputItemType=", // Multi-line Generator ref attributes
        "ReferenceOutputAssembly=",
    ];

    /// <summary>
    /// Transform the Host .csproj: strip module ProjectReferences, ServiceDefaults,
    /// InternalsVisibleTo, NoWarn/SM0011/SM0035/SM0038/TODO lines, EF Core Design package,
    /// and the Import Project line. Replace framework ProjectReferences with PackageReferences.
    /// Replace SimpleModule with projectName.
    /// </summary>
    public static string HostCsproj(string projectName)
    {
        var lines = EmbeddedResourceReader.ReadTemplateLines(
            "Templates.Host.SimpleModule.Host.csproj"
        );

        // Strip lines containing patterns that should not appear in a new project
        lines.RemoveAll(line =>
            CsprojStripPatterns.Any(p => line.Contains(p, StringComparison.Ordinal))
        );

        // Remove orphaned multi-line XML fragments (e.g., <ProjectReference\n/> after attribute stripping)
        lines.RemoveAll(line =>
        {
            var trimmed = line.TrimStart();
            return trimmed == "/>"
                || (
                    trimmed.StartsWith("<ProjectReference", StringComparison.Ordinal)
                    && !trimmed.Contains("Include=", StringComparison.Ordinal)
                );
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
    /// Copy Program.cs, remove ServiceDefaults, MapDefaultEndpoints, and Storage.Local lines.
    /// Framework method names (AddSimpleModule, UseSimpleModule) are kept as-is.
    /// </summary>
    public static string ProgramCs()
    {
        var lines = EmbeddedResourceReader.ReadTemplateLines("Templates.Host.Program.cs");

        lines.RemoveAll(line =>
            line.Contains("ServiceDefaults", StringComparison.Ordinal)
            || line.Contains("MapDefaultEndpoints", StringComparison.Ordinal)
            || line.Contains("Storage.Local", StringComparison.Ordinal)
            || line.Contains("AddLocalStorage", StringComparison.Ordinal)
        );

        lines = TemplateExtractor.CollapseBlankLines(lines);

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Copy App.razor, replace SimpleModule with projectName.
    /// </summary>
    public static string AppRazor(string projectName)
    {
        var content = EmbeddedResourceReader.ReadTemplate("Templates.Host.Components.App.razor");
        return ReplaceProjectName(content, projectName);
    }

    /// <summary>
    /// Copy InertiaShell.razor, replace SimpleModule with projectName.
    /// </summary>
    public static string InertiaShellRazor(string projectName)
    {
        var content = EmbeddedResourceReader.ReadTemplate(
            "Templates.Host.Components.InertiaShell.razor"
        );
        return ReplaceProjectName(content, projectName);
    }

    /// <summary>
    /// Copy Routes.razor, stripping AdditionalAssemblies references and
    /// closing the Router tag properly.
    /// </summary>
    public static string RoutesRazor()
    {
        var lines = EmbeddedResourceReader.ReadTemplateLines(
            "Templates.Host.Components.Routes.razor"
        );
        lines.RemoveAll(line => line.Contains("AdditionalAssemblies", StringComparison.Ordinal));

        // Fix unclosed Router tag: replace trailing open attribute list with closing >
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimEnd();
            if (trimmed.Contains("<Router", StringComparison.Ordinal) && !trimmed.EndsWith('>'))
            {
                lines[i] = trimmed + ">";
            }
        }

        lines = TemplateExtractor.CollapseBlankLines(lines);

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Copy _Imports.razor, replace SimpleModule with projectName.
    /// </summary>
    public static string ImportsRazor(string projectName)
    {
        var content = EmbeddedResourceReader.ReadTemplate(
            "Templates.Host.Components._Imports.razor"
        );
        return ReplaceProjectName(content, projectName);
    }

    /// <summary>
    /// Copy app.tsx as-is.
    /// </summary>
    public static string AppTsx()
    {
        return EmbeddedResourceReader.ReadTemplate("Templates.Host.ClientApp.app.tsx");
    }

    /// <summary>
    /// Copy vite.config.ts as-is.
    /// </summary>
    public static string ViteConfig()
    {
        return EmbeddedResourceReader.ReadTemplate("Templates.Host.ClientApp.vite.config.ts");
    }

    /// <summary>
    /// Copy validate-pages.mjs as-is.
    /// </summary>
    public static string ValidatePages()
    {
        return EmbeddedResourceReader.ReadTemplate("Templates.Host.ClientApp.validate-pages.mjs");
    }

    /// <summary>
    /// Copy ClientApp/package.json, replace @simplemodule/app with @{projectName}/app.
    /// </summary>
    public static string ClientAppPackageJson(string projectName)
    {
        var content = EmbeddedResourceReader.ReadTemplate("Templates.Host.ClientApp.package.json");
        return content.Replace(
            "@simplemodule/app",
            $"@{projectName.ToLowerInvariant()}/app",
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Return a clean Styles/app.css with tailwindcss import, theme import,
    /// and @source directives for modules. Strip _scan/ import if present.
    /// </summary>
    public static string AppCss()
    {
        var lines = EmbeddedResourceReader.ReadTemplateLines("Templates.Host.Styles.app.css");
        lines.RemoveAll(line => line.Contains("_scan/", StringComparison.Ordinal));

        // Replace module source paths for new project structure (template/ → src/)
        var result = string.Join(Environment.NewLine, lines);
        result = result.Replace("../../modules/", "../../../modules/", StringComparison.Ordinal);

        return result;
    }

    /// <summary>
    /// Copy appsettings.json as-is.
    /// </summary>
    public static string AppSettings()
    {
        return EmbeddedResourceReader.ReadTemplate("Templates.Host.appsettings.json");
    }

    /// <summary>
    /// Copy appsettings.Development.json as-is.
    /// </summary>
    public static string AppSettingsDevelopment()
    {
        return EmbeddedResourceReader.ReadTemplate("Templates.Host.appsettings.Development.json");
    }

    /// <summary>
    /// Copy launchSettings.json, replace SimpleModule with projectName.
    /// </summary>
    public static string LaunchSettings(string projectName)
    {
        var content = EmbeddedResourceReader.ReadTemplate(
            "Templates.Host.Properties.launchSettings.json"
        );
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
            content = content.Replace(
                frameworkNames[i],
                placeholders[i].Placeholder,
                StringComparison.Ordinal
            );
        }

        // Replace the project name
        content = content.Replace(BaseProjectName, projectName, StringComparison.Ordinal);

        // Restore framework names
        for (var i = 0; i < placeholders.Length; i++)
        {
            content = content.Replace(
                placeholders[i].Placeholder,
                placeholders[i].Original,
                StringComparison.Ordinal
            );
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
