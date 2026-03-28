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

    private readonly string? _templateHostDir;

    public HostTemplates(SolutionContext? solution)
    {
        _templateHostDir = solution is not null
            ? Path.Combine(solution.RootPath, "template", "SimpleModule.Host")
            : null;
    }

    /// <summary>
    /// Transform the Host .csproj: strip module ProjectReferences, ServiceDefaults,
    /// InternalsVisibleTo, NoWarn/SM0011/SM0035/SM0038/TODO lines, EF Core Design package,
    /// and the Import Project line. Replace framework ProjectReferences with PackageReferences.
    /// Replace SimpleModule with projectName.
    /// </summary>
    public string HostCsproj(string projectName)
    {
        if (_templateHostDir is null)
        {
            return FallbackHostCsproj(projectName);
        }

        var path = Path.Combine(_templateHostDir, "SimpleModule.Host.csproj");
        if (!File.Exists(path))
        {
            return FallbackHostCsproj(projectName);
        }

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
        if (_templateHostDir is null)
        {
            return FallbackProgramCs();
        }

        var path = Path.Combine(_templateHostDir, "Program.cs");
        if (!File.Exists(path))
        {
            return FallbackProgramCs();
        }

        var lines = File.ReadAllLines(path).ToList();

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
    public string AppRazor(string projectName)
    {
        if (_templateHostDir is null)
        {
            return FallbackAppRazor(projectName);
        }

        var path = Path.Combine(_templateHostDir, "Components", "App.razor");
        if (!File.Exists(path))
        {
            return FallbackAppRazor(projectName);
        }

        var content = File.ReadAllText(path);
        return ReplaceProjectName(content, projectName);
    }

    /// <summary>
    /// Copy InertiaShell.razor, replace SimpleModule with projectName.
    /// </summary>
    public string InertiaShellRazor(string projectName)
    {
        if (_templateHostDir is null)
        {
            return FallbackInertiaShellRazor(projectName);
        }

        var path = Path.Combine(_templateHostDir, "Components", "InertiaShell.razor");
        if (!File.Exists(path))
        {
            return FallbackInertiaShellRazor(projectName);
        }

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
        if (_templateHostDir is null)
        {
            return FallbackImportsRazor(projectName);
        }

        var path = Path.Combine(_templateHostDir, "Components", "_Imports.razor");
        if (!File.Exists(path))
        {
            return FallbackImportsRazor(projectName);
        }

        var content = File.ReadAllText(path);
        return ReplaceProjectName(content, projectName);
    }

    /// <summary>
    /// Copy app.tsx as-is.
    /// </summary>
    public string AppTsx()
    {
        if (_templateHostDir is null)
        {
            return FallbackAppTsx();
        }

        var path = Path.Combine(_templateHostDir, "ClientApp", "app.tsx");
        return File.Exists(path) ? File.ReadAllText(path) : FallbackAppTsx();
    }

    /// <summary>
    /// Copy vite.config.ts as-is.
    /// </summary>
    public string ViteConfig()
    {
        if (_templateHostDir is null)
        {
            return FallbackViteConfig();
        }

        var path = Path.Combine(_templateHostDir, "ClientApp", "vite.config.ts");
        return File.Exists(path) ? File.ReadAllText(path) : FallbackViteConfig();
    }

    /// <summary>
    /// Copy validate-pages.mjs as-is.
    /// </summary>
    public string ValidatePages()
    {
        if (_templateHostDir is null)
        {
            return FallbackValidatePages();
        }

        var path = Path.Combine(_templateHostDir, "ClientApp", "validate-pages.mjs");
        return File.Exists(path) ? File.ReadAllText(path) : FallbackValidatePages();
    }

    /// <summary>
    /// Copy ClientApp/package.json, replace @simplemodule/app with @{projectName}/app.
    /// </summary>
    public string ClientAppPackageJson(string projectName)
    {
        if (_templateHostDir is null)
        {
            return FallbackClientAppPackageJson(projectName);
        }

        var path = Path.Combine(_templateHostDir, "ClientApp", "package.json");
        if (!File.Exists(path))
        {
            return FallbackClientAppPackageJson(projectName);
        }

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
        if (_templateHostDir is null)
        {
            return FallbackAppSettings();
        }

        var path = Path.Combine(_templateHostDir, "appsettings.json");
        return File.Exists(path) ? File.ReadAllText(path) : FallbackAppSettings();
    }

    /// <summary>
    /// Copy appsettings.Development.json as-is.
    /// </summary>
    public string AppSettingsDevelopment()
    {
        if (_templateHostDir is null)
        {
            return FallbackAppSettingsDevelopment();
        }

        var path = Path.Combine(_templateHostDir, "appsettings.Development.json");
        return File.Exists(path) ? File.ReadAllText(path) : FallbackAppSettingsDevelopment();
    }

    /// <summary>
    /// Copy launchSettings.json, replace SimpleModule with projectName.
    /// </summary>
    public string LaunchSettings(string projectName)
    {
        if (_templateHostDir is null)
        {
            return FallbackLaunchSettings(projectName);
        }

        var path = Path.Combine(_templateHostDir, "Properties", "launchSettings.json");
        if (!File.Exists(path))
        {
            return FallbackLaunchSettings(projectName);
        }

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

    // ── Fallback templates ────────────────────────────────────────────

    private static string FallbackHostCsproj(string projectName) =>
        $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
                <PackageReference Include="Swashbuckle.AspNetCore" />
                <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
              </ItemGroup>
              <ItemGroup>
                <PackageReference Include="SimpleModule.Hosting" />
                <PackageReference Include="SimpleModule.Generator" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackProgramCs() =>
        """
            using SimpleModule.Hosting;

            var builder = WebApplication.CreateBuilder(args);

            builder.AddSimpleModule();

            var app = builder.Build();

            await app.UseSimpleModule();

            await app.RunAsync();
            """;

    private static string FallbackAppRazor(string projectName) =>
        $$"""
            <!DOCTYPE html>
            <html lang="en" class="dark">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{{projectName}}</title>
                <link rel="stylesheet" href="css/app.css" />
                <HeadOutlet />
            </head>
            <body>
                <Routes />
                <script src="js/app.js"></script>
            </body>
            </html>
            """;

    private static string FallbackInertiaShellRazor(string projectName) =>
        $$"""
            @using Microsoft.AspNetCore.Components
            @using Microsoft.AspNetCore.Components.Web

            <div id="app" data-page="@PageJson"></div>

            @code {
                [Parameter] public string PageJson { get; set; } = string.Empty;
            }
            """;

    private static string FallbackImportsRazor(string projectName) =>
        $$"""
            @using Microsoft.AspNetCore.Components.Routing
            @using Microsoft.AspNetCore.Components.Web
            @using {{projectName}}.Host.Components
            """;

    private static string FallbackAppTsx() =>
        """
            import { createInertiaApp } from '@inertiajs/react';
            import { createRoot } from 'react-dom/client';
            import { resolvePage } from '@simplemodule/client/resolve-page';

            createInertiaApp({
              resolve: resolvePage,
              setup({ el, App, props }) {
                createRoot(el).render(<App {...props} />);
              },
            });
            """;

    private static string FallbackViteConfig() =>
        """
            import { defineConfig } from 'vite';
            import react from '@vitejs/plugin-react';
            import { simpleModuleVendor } from '@simplemodule/client/vite';

            export default defineConfig({
              plugins: [simpleModuleVendor(), react()],
              build: {
                rollupOptions: {
                  input: 'app.tsx',
                  output: {
                    entryFileNames: 'js/app.js',
                    dir: '../wwwroot',
                  },
                },
                sourcemap: process.env.VITE_MODE !== 'prod',
                minify: process.env.VITE_MODE === 'prod',
              },
            });
            """;

    private static string FallbackValidatePages() =>
        """
            // Placeholder: validate-pages script
            // Run this to check that all C# IViewEndpoints have matching page entries
            console.log('validate-pages: OK');
            """;

    private static string FallbackClientAppPackageJson(string projectName)
    {
        var name = projectName.ToLowerInvariant();
        return $$"""
            {
              "private": true,
              "name": "@{{name}}/app",
              "version": "0.0.0",
              "scripts": {
                "build": "vite build",
                "build:dev": "cross-env VITE_MODE=dev vite build",
                "watch": "cross-env VITE_MODE=dev vite build --watch",
                "validate-pages": "node validate-pages.mjs"
              },
              "dependencies": {
                "@inertiajs/react": "^2.0.0",
                "react": "^19.0.0",
                "react-dom": "^19.0.0"
              }
            }
            """;
    }

    private static string FallbackAppSettings() =>
        """
            {
              "Database": {
                "DefaultConnection": "Data Source=app.db"
              },
              "Storage": {
                "Provider": "Local",
                "Local": {
                  "Path": "./storage"
                }
              },
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning"
                }
              }
            }
            """;

    private static string FallbackAppSettingsDevelopment() =>
        """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.EntityFrameworkCore.Database.Command": "Information"
                },
                "Console": {
                  "FormatterName": "simple",
                  "FormatterOptions": {
                    "TimestampFormat": "HH:mm:ss ",
                    "SingleLine": true
                  }
                }
              }
            }
            """;

    private static string FallbackLaunchSettings(string projectName) =>
        $$"""
            {
              "profiles": {
                "{{projectName}}.Host": {
                  "commandName": "Project",
                  "launchBrowser": false,
                  "applicationUrl": "https://localhost:5001;http://localhost:5000",
                  "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                  }
                }
              }
            }
            """;
}
