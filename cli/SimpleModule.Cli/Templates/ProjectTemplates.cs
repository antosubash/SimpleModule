using System.Text.RegularExpressions;
using System.Xml.Linq;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Templates;

public sealed class ProjectTemplates
{
    private const string BaseProjectName = "SimpleModule";
    private readonly SolutionContext? _solution;

    public ProjectTemplates(SolutionContext? solution)
    {
        _solution = solution;
    }

    public string Slnx(string projectName)
    {
        if (_solution is null)
        {
            return FallbackSlnx(projectName);
        }

        var lines = File.ReadAllLines(_solution.SlnxPath).ToList();

        // Strip all module-related folders and test module entries
        lines.RemoveAll(line =>
            line.Contains("/modules/", StringComparison.Ordinal)
            && !line.TrimStart().StartsWith("<Folder Name=\"/modules/\"", StringComparison.Ordinal)
        );

        // Strip /tests/modules/ folder entirely (module tests are now inside each module)
        lines.RemoveAll(line => line.Contains("/tests/modules/", StringComparison.Ordinal));

        // Remove CLI project entry
        lines.RemoveAll(line => line.Contains("SimpleModule.Cli", StringComparison.Ordinal));

        lines = TemplateExtractor.CollapseBlankLines(lines);

        var content = string.Join(Environment.NewLine, lines);
        return content.Replace(BaseProjectName, projectName, StringComparison.Ordinal);
    }

    public string DirectoryBuildProps()
    {
        if (_solution is null)
        {
            return FallbackDirectoryBuildProps();
        }

        var path = Path.Combine(_solution.RootPath, "Directory.Build.props");
        if (!File.Exists(path))
        {
            return FallbackDirectoryBuildProps();
        }

        var content = File.ReadAllText(path);

        // Strip Roslynator ItemGroup (project-specific analyzer)
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
        lines = TemplateExtractor.RemoveBraceBlocks(lines, _ => false); // no-op, just for consistency

        // Remove Roslynator-related lines
        var result = new List<string>();
        var skipItemGroup = false;

        foreach (var line in lines)
        {
            if (!skipItemGroup && line.Contains("Roslynator", StringComparison.Ordinal))
            {
                // Find the containing ItemGroup - walk back to remove it
                // Instead, mark for removal
                skipItemGroup = true;
                // Remove the ItemGroup start we already added
                while (
                    result.Count > 0
                    && !result[^1].Contains("<ItemGroup>", StringComparison.Ordinal)
                )
                {
                    result.RemoveAt(result.Count - 1);
                }

                if (
                    result.Count > 0
                    && result[^1].Contains("<ItemGroup>", StringComparison.Ordinal)
                )
                {
                    result.RemoveAt(result.Count - 1);
                }

                continue;
            }

            if (skipItemGroup)
            {
                if (line.Contains("</ItemGroup>", StringComparison.Ordinal))
                {
                    skipItemGroup = false;
                }

                continue;
            }

            result.Add(line);
        }

        return string.Join(Environment.NewLine, TemplateExtractor.CollapseBlankLines(result));
    }

    public string DirectoryPackagesProps()
    {
        if (_solution is null)
        {
            return FallbackDirectoryPackagesProps();
        }

        var path = Path.Combine(_solution.RootPath, "Directory.Packages.props");
        if (!File.Exists(path))
        {
            return FallbackDirectoryPackagesProps();
        }

        // Packages to strip (project-specific, not needed for a fresh project)
        var stripPackages = new[]
        {
            "Roslynator",
            "Bogus",
            "OpenIddict",
            "Identity",
            "Spectre.Console",
            "Microsoft.EntityFrameworkCore.Design",
            "Microsoft.EntityFrameworkCore.SqlServer",
        };

        var lines = File.ReadAllLines(path).ToList();

        // Remove lines containing stripped packages
        lines.RemoveAll(line =>
            stripPackages.Any(p => line.Contains(p, StringComparison.OrdinalIgnoreCase))
        );

        // Remove comment lines that now have no following package entries
        // (e.g., "<!-- Identity -->" with nothing after it)
        var cleaned = new List<string>();
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (
                trimmed.StartsWith("<!--", StringComparison.Ordinal)
                && trimmed.EndsWith("-->", StringComparison.Ordinal)
            )
            {
                // Check if the next non-blank line is another comment or closing tag
                var nextContentLine = lines
                    .Skip(i + 1)
                    .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));

                if (
                    nextContentLine is not null
                    && (
                        nextContentLine.TrimStart().StartsWith("<!--", StringComparison.Ordinal)
                        || nextContentLine.TrimStart().StartsWith("</", StringComparison.Ordinal)
                    )
                )
                {
                    continue; // Skip orphaned comment
                }
            }

            cleaned.Add(lines[i]);
        }

        // Add SimpleModule framework package versions before the closing </ItemGroup>
        var frameworkPackages = new[]
        {
            "    <!-- SimpleModule Framework -->",
            "    <PackageVersion Include=\"SimpleModule.Core\" Version=\"0.1.0-local\" />",
            "    <PackageVersion Include=\"SimpleModule.Database\" Version=\"0.1.0-local\" />",
            "    <PackageVersion Include=\"SimpleModule.Hosting\" Version=\"0.1.0-local\" />",
            "    <PackageVersion Include=\"SimpleModule.Generator\" Version=\"0.1.0-local\" />",
        };

        // Find the last </ItemGroup> and insert before it
        var lastItemGroupClose = cleaned.FindLastIndex(l =>
            l.TrimStart().StartsWith("</ItemGroup>", StringComparison.Ordinal));
        if (lastItemGroupClose >= 0)
        {
            cleaned.InsertRange(lastItemGroupClose, frameworkPackages);
        }

        return string.Join(Environment.NewLine, TemplateExtractor.CollapseBlankLines(cleaned));
    }

    public static string NugetConfig(string simpleModuleRepoPath)
    {
        var nupkgPath = Path.Combine(simpleModuleRepoPath, "nupkg").Replace('\\', '/');
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                <add key="SimpleModule-Local" value="{nupkgPath}" />
              </packageSources>
            </configuration>
            """;
    }

    public string GlobalJson()
    {
        if (_solution is null)
        {
            return FallbackGlobalJson();
        }

        var path = Path.Combine(_solution.RootPath, "global.json");
        return File.Exists(path) ? File.ReadAllText(path) : FallbackGlobalJson();
    }

    public static string RootPackageJson(string projectName, string frameworkPackagesPath)
    {
        return FallbackRootPackageJson(projectName, frameworkPackagesPath);
    }

    public string BiomeJson()
    {
        if (_solution is null)
        {
            return FallbackBiomeJson();
        }

        var path = Path.Combine(_solution.RootPath, "biome.json");
        if (!File.Exists(path))
        {
            return FallbackBiomeJson();
        }

        var content = File.ReadAllText(path);

        // Replace file includes: monorepo paths → project paths
        content = content.Replace(
            "\"modules/**\", \"packages/**\", \"template/**\", \"tests/**\"",
            "\"src/**\", \"tests/**\"",
            StringComparison.Ordinal
        );

        return content;
    }

    public string TsconfigJson()
    {
        if (_solution is null)
        {
            return FallbackTsconfigJson();
        }

        var path = Path.Combine(_solution.RootPath, "tsconfig.json");
        if (!File.Exists(path))
        {
            return FallbackTsconfigJson();
        }

        var content = File.ReadAllText(path);

        // Remove the SimpleModule.UI registry templates exclude entry
        content = content.Replace(
            ", \"src/SimpleModule.UI/registry/templates\"",
            "",
            StringComparison.Ordinal
        );
        content = content.Replace(
            "\"src/SimpleModule.UI/registry/templates\", ",
            "",
            StringComparison.Ordinal
        );
        content = content.Replace(
            "\"src/SimpleModule.UI/registry/templates\"",
            "",
            StringComparison.Ordinal
        );

        return content;
    }

    public string EditorConfig()
    {
        if (_solution is null)
        {
            return string.Empty;
        }

        var path = Path.Combine(_solution.RootPath, ".editorconfig");
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var content = File.ReadAllText(path);
        // Adjust test path glob for new project structure (modules under src/)
        content = content.Replace(
            "{tests,modules/*/tests}",
            "{tests,src/modules/*/tests}",
            StringComparison.Ordinal
        );
        return content;
    }

    public string ApiCsproj(string projectName)
    {
        if (_solution is null)
        {
            return FallbackApiCsproj(projectName);
        }

        // Strip module references, OpenIddict, InternalsVisibleTo, Tailwind, Blazor-related
        var stripPatterns = new List<string>
        {
            "modules",
            "OpenIddict",
            "InternalsVisibleTo",
            "Tailwind",
        };

        var content = TemplateExtractor.TransformCsproj(
            _solution.ApiCsprojPath,
            BaseProjectName,
            projectName,
            stripPatterns
        );

        // Remove Tailwind targets (text-based since TransformCsproj only handles ItemGroup elements)
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
        var result = new List<string>();
        var skipBlock = false;

        foreach (var line in lines)
        {
            if (
                line.Contains("Tailwind", StringComparison.Ordinal)
                || line.Contains("TailwindCli", StringComparison.Ordinal)
            )
            {
                skipBlock = true;
                continue;
            }

            if (skipBlock)
            {
                if (
                    line.TrimStart().StartsWith("</Target>", StringComparison.Ordinal)
                    || line.TrimStart().StartsWith("</PropertyGroup>", StringComparison.Ordinal)
                )
                {
                    skipBlock = false;
                    continue;
                }

                continue;
            }

            result.Add(line);
        }

        // Remove InvariantGlobalization line
        result.RemoveAll(line => line.Contains("InvariantGlobalization", StringComparison.Ordinal));

        return string.Join(Environment.NewLine, TemplateExtractor.CollapseBlankLines(result));
    }

    public static string ApiProgram()
    {
        // The actual Program.cs is too complex (auth, Blazor, health checks, etc.)
        // For a new project, provide a clean minimal version
        return """
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddModules(builder.Configuration);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapModuleEndpoints();

            app.Run();
            """;
    }

    public string CoreCsproj(string projectName)
    {
        if (_solution is null)
        {
            return FallbackCoreCsproj();
        }

        var path = Path.Combine(
            _solution.RootPath,
            "src",
            $"{BaseProjectName}.Core",
            $"{BaseProjectName}.Core.csproj"
        );
        if (!File.Exists(path))
        {
            return FallbackCoreCsproj();
        }

        return File.ReadAllText(path);
    }

    public string DatabaseCsproj(string projectName)
    {
        if (_solution is null)
        {
            return FallbackDatabaseCsproj();
        }

        var path = Path.Combine(
            _solution.RootPath,
            "src",
            $"{BaseProjectName}.Database",
            $"{BaseProjectName}.Database.csproj"
        );
        if (!File.Exists(path))
        {
            return FallbackDatabaseCsproj();
        }

        var content = File.ReadAllText(path);
        return content.Replace(BaseProjectName, projectName, StringComparison.Ordinal);
    }

    public string GeneratorCsproj()
    {
        if (_solution is null)
        {
            return FallbackGeneratorCsproj();
        }

        var path = Path.Combine(
            _solution.RootPath,
            "src",
            $"{BaseProjectName}.Generator",
            $"{BaseProjectName}.Generator.csproj"
        );
        return File.Exists(path) ? File.ReadAllText(path) : FallbackGeneratorCsproj();
    }

    public string TestsSharedCsproj(string projectName)
    {
        if (_solution is null)
        {
            return FallbackTestsSharedCsproj(projectName);
        }

        var path = Path.Combine(
            _solution.RootPath,
            "tests",
            $"{BaseProjectName}.Tests.Shared",
            $"{BaseProjectName}.Tests.Shared.csproj"
        );
        if (!File.Exists(path))
        {
            return FallbackTestsSharedCsproj(projectName);
        }

        // Strip module contract references, OpenIddict, Bogus
        var stripPatterns = new List<string> { "modules", "OpenIddict", "Bogus" };
        var result = TemplateExtractor.TransformCsproj(path, BaseProjectName, projectName, stripPatterns);
        // Fix Host path: template\ → src\ (in repo it's under template/, in generated project it's under src/)
        return result.Replace(@"template\", @"src\", StringComparison.Ordinal);
    }

    // ── Fallback templates ──────────────────────────────────────────

    private static string FallbackSlnx(string projectName) =>
        $"""
            <Solution>
                <Configurations>
                    <Platform Name="Any CPU" />
                    <Platform Name="x64" />
                    <Platform Name="x86" />
                </Configurations>
                <Folder Name="/src/">
                    <Project Path="src/{projectName}.Api/{projectName}.Api.csproj" />
                    <Project Path="src/{projectName}.Core/{projectName}.Core.csproj" />
                    <Project Path="src/{projectName}.Database/{projectName}.Database.csproj" />
                    <Project Path="src/{projectName}.Generator/{projectName}.Generator.csproj" />
                </Folder>
                <Folder Name="/modules/" />
                <Folder Name="/tests/">
                    <Project Path="tests/{projectName}.Tests.Shared/{projectName}.Tests.Shared.csproj" />
                </Folder>
            </Solution>
            """;

    private static string FallbackDirectoryBuildProps() =>
        """
            <Project>
              <PropertyGroup>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
              </PropertyGroup>
              <PropertyGroup Condition="'$(TargetFramework)' != 'netstandard2.0'">
                <AnalysisLevel>latest-all</AnalysisLevel>
                <AnalysisMode>All</AnalysisMode>
              </PropertyGroup>
            </Project>
            """;

    private static string FallbackDirectoryPackagesProps() =>
        """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <!-- SimpleModule Framework -->
                <PackageVersion Include="SimpleModule.Core" Version="0.1.0-local" />
                <PackageVersion Include="SimpleModule.Database" Version="0.1.0-local" />
                <PackageVersion Include="SimpleModule.Hosting" Version="0.1.0-local" />
                <PackageVersion Include="SimpleModule.Generator" Version="0.1.0-local" />
                <!-- Source Generator -->
                <PackageVersion Include="Microsoft.CodeAnalysis.Analyzers" Version="4.14.0" />
                <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="5.0.0" />
                <!-- EF Core -->
                <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.3" />
                <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
                <!-- API -->
                <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.3" />
                <PackageVersion Include="Swashbuckle.AspNetCore" Version="10.1.4" />
                <!-- Testing -->
                <PackageVersion Include="xunit.v3" Version="1.1.0" />
                <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.0" />
                <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
                <PackageVersion Include="FluentAssertions" Version="8.3.0" />
                <PackageVersion Include="NSubstitute" Version="5.3.0" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackGlobalJson() =>
        """
            {
              "sdk": {
                "version": "10.0.100",
                "rollForward": "latestMinor"
              }
            }
            """;

    private static string FallbackApiCsproj(string projectName) =>
        $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
                <PackageReference Include="Swashbuckle.AspNetCore" />
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="..\{projectName}.Core\{projectName}.Core.csproj" />
                <ProjectReference Include="..\{projectName}.Database\{projectName}.Database.csproj" />
                <ProjectReference
                  Include="..\{projectName}.Generator\{projectName}.Generator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackCoreCsproj() =>
        """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Library</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackDatabaseCsproj() =>
        """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Library</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
                <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackGeneratorCsproj() =>
        """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>netstandard2.0</TargetFramework>
                <LangVersion>latest</LangVersion>
                <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" />
                <PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackTestsSharedCsproj(string projectName) =>
        $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
              </ItemGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="..\..\src\{projectName}.Host\{projectName}.Host.csproj" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackRootPackageJson(string projectName, string frameworkPackagesPath)
    {
        var name = projectName.ToLowerInvariant();
        var pkgPath = frameworkPackagesPath.Replace('\\', '/');
        return $$"""
            {
              "private": true,
              "name": "{{name}}",
              "version": "0.0.0",
              "workspaces": [
                "src/modules/*/src/*",
                "src/{{name}}.Host/ClientApp"
              ],
              "scripts": {
                "lint": "biome lint .",
                "format": "biome format --write .",
                "check": "biome check .",
                "check:fix": "biome check --write .",
                "build": "cross-env VITE_MODE=prod npm run build --workspaces --if-present",
                "build:dev": "cross-env VITE_MODE=dev npm run build --workspaces --if-present"
              },
              "devDependencies": {
                "@biomejs/biome": "^2.4.6",
                "@tailwindcss/vite": "^4.2.1",
                "@types/react": "^19.0.0",
                "@types/react-dom": "^19.0.0",
                "@vitejs/plugin-react": "^4.4.0",
                "cross-env": "^7.0.3",
                "typescript": "^5.8.0",
                "vite": "^6.2.0"
              },
              "dependencies": {
                "@inertiajs/react": "^2.0.0",
                "@simplemodule/client": "file:{{pkgPath}}/SimpleModule.Client",
                "@simplemodule/ui": "file:{{pkgPath}}/SimpleModule.UI",
                "@simplemodule/theme-default": "file:{{pkgPath}}/SimpleModule.Theme.Default",
                "react": "^19.0.0",
                "react-dom": "^19.0.0",
                "tailwindcss": "^4.2.1"
              }
            }
            """;
    }

    private static string FallbackBiomeJson() =>
        """
            {
              "$schema": "https://biomejs.dev/schemas/2.4.7/schema.json",
              "vcs": {
                "enabled": true,
                "clientKind": "git",
                "useIgnoreFile": true
              },
              "formatter": {
                "enabled": true,
                "indentStyle": "space",
                "indentWidth": 2,
                "lineWidth": 100
              },
              "css": {
                "parser": {
                  "tailwindDirectives": true
                }
              },
              "javascript": {
                "formatter": {
                  "quoteStyle": "single",
                  "trailingCommas": "all",
                  "semicolons": "always"
                }
              },
              "linter": {
                "enabled": true,
                "rules": {
                  "recommended": true
                }
              },
              "files": {
                "includes": ["src/**", "tests/**", "!**/wwwroot"]
              }
            }
            """;

    private static string FallbackTsconfigJson() =>
        """
            {
              "compilerOptions": {
                "target": "ES2022",
                "module": "ESNext",
                "moduleResolution": "bundler",
                "jsx": "react-jsx",
                "strict": true,
                "esModuleInterop": true,
                "skipLibCheck": true,
                "forceConsistentCasingInFileNames": true,
                "resolveJsonModule": true,
                "isolatedModules": true,
                "noEmit": true
              },
              "exclude": ["node_modules", "**/wwwroot/**"]
            }
            """;
}
