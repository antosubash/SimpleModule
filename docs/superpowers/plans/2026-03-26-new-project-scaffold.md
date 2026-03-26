# Improved `sm new project` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite `sm new project` to scaffold a working project by copying the template Host, adding a "Home" starter module with tests, and creating all root config files.

**Architecture:** The command copies `template/SimpleModule.Host/` with `SimpleModule`→`{ProjectName}` transforms, generates root config files (slnx, package.json, biome.json, etc.), then reuses `ModuleTemplates` to create a "Home" module. The new project references the framework via relative project references.

**Tech Stack:** C# / Spectre.Console.Cli / existing TemplateExtractor + SlnxManipulator + ProjectManipulator infrastructure

---

### Task 1: Add `HostTemplates` class for Host project file generation

**Files:**
- Create: `cli/SimpleModule.Cli/Templates/HostTemplates.cs`

This class reads template Host files and transforms them for the new project.

- [ ] **Step 1: Create `HostTemplates.cs`**

```csharp
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Templates;

public sealed class HostTemplates
{
    private readonly SolutionContext _solution;
    private readonly string _templateHostDir;

    public HostTemplates(SolutionContext solution)
    {
        _solution = solution;
        _templateHostDir = Path.Combine(solution.RootPath, "template", "SimpleModule.Host");
    }

    /// <summary>
    /// Transforms the Host .csproj: strips all module refs, ServiceDefaults, InternalsVisibleTo,
    /// NoWarn, EF Design, and the Hosting.targets import. Keeps framework refs with adjusted paths.
    /// </summary>
    public string HostCsproj(string projectName, string frameworkRelativePath)
    {
        var path = Path.Combine(_templateHostDir, "SimpleModule.Host.csproj");
        var lines = File.ReadAllLines(path).ToList();

        // Strip lines containing these patterns
        var stripPatterns = new[]
        {
            "modules",
            "ServiceDefaults",
            "InternalsVisibleTo",
            "NoWarn",
            "SM0011",
            "SM0035",
            "SM0038",
            "TODO:",
            "EntityFrameworkCore.Design",
            "Import Project",
        };

        lines.RemoveAll(line =>
            stripPatterns.Any(p => line.Contains(p, StringComparison.Ordinal))
        );

        // Remove empty PropertyGroup/ItemGroup elements
        var cleaned = new List<string>();
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            // Check for empty element pairs (opening tag followed immediately by closing tag)
            if (i + 1 < lines.Count)
            {
                var nextTrimmed = lines[i + 1].TrimStart();
                if (
                    (trimmed == "<PropertyGroup>" && nextTrimmed == "</PropertyGroup>")
                    || (trimmed == "<ItemGroup>" && nextTrimmed == "</ItemGroup>")
                )
                {
                    i++; // Skip both lines
                    continue;
                }
            }

            cleaned.Add(lines[i]);
        }

        lines = TemplateExtractor.CollapseBlankLines(cleaned);

        // Adjust framework project reference paths
        var content = string.Join(Environment.NewLine, lines);
        content = content.Replace(
            @"..\..\framework\SimpleModule.Hosting\SimpleModule.Hosting.csproj",
            Path.Combine(frameworkRelativePath, "SimpleModule.Hosting", "SimpleModule.Hosting.csproj"),
            StringComparison.Ordinal
        );
        content = content.Replace(
            @"..\..\framework\SimpleModule.Generator\SimpleModule.Generator.csproj",
            Path.Combine(frameworkRelativePath, "SimpleModule.Generator", "SimpleModule.Generator.csproj"),
            StringComparison.Ordinal
        );

        // Rename SimpleModule → ProjectName
        content = content.Replace("SimpleModule", projectName, StringComparison.Ordinal);

        return content;
    }

    /// <summary>
    /// Program.cs — copy as-is (the generated extension methods handle everything).
    /// </summary>
    public string ProgramCs(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Program.cs");
        var content = File.ReadAllText(path);

        // Remove Aspire ServiceDefaults lines
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
        lines.RemoveAll(line =>
            line.Contains("ServiceDefaults", StringComparison.Ordinal)
            || line.Contains("MapDefaultEndpoints", StringComparison.Ordinal)
        );
        lines = TemplateExtractor.CollapseBlankLines(lines);

        content = string.Join(Environment.NewLine, lines);
        return content.Replace("SimpleModule", projectName, StringComparison.Ordinal);
    }

    /// <summary>
    /// App.razor — replace title and namespace references.
    /// </summary>
    public string AppRazor(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Components", "App.razor");
        var content = File.ReadAllText(path);
        return content.Replace("SimpleModule", projectName, StringComparison.Ordinal);
    }

    /// <summary>
    /// InertiaShell.razor — copy as-is (uses framework components).
    /// </summary>
    public string InertiaShellRazor(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Components", "InertiaShell.razor");
        var content = File.ReadAllText(path);
        return content.Replace("SimpleModule", projectName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Routes.razor — remove module-specific assembly references.
    /// </summary>
    public string RoutesRazor(string projectName)
    {
        // Generate a clean Routes.razor without module assembly references
        return $$"""
            <Router AppAssembly="typeof(App).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="routeData" DefaultLayout="typeof({{projectName}}.Blazor.Components.Layout.MainLayout)" />
                </Found>
            </Router>
            """;
    }

    /// <summary>
    /// _Imports.razor — replace namespace.
    /// </summary>
    public string ImportsRazor(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Components", "_Imports.razor");
        var content = File.ReadAllText(path);
        return content.Replace("SimpleModule", projectName, StringComparison.Ordinal);
    }

    /// <summary>
    /// ClientApp/app.tsx — copy as-is.
    /// </summary>
    public string AppTsx()
    {
        return File.ReadAllText(Path.Combine(_templateHostDir, "ClientApp", "app.tsx"));
    }

    /// <summary>
    /// ClientApp/vite.config.ts — copy as-is.
    /// </summary>
    public string ViteConfig()
    {
        return File.ReadAllText(Path.Combine(_templateHostDir, "ClientApp", "vite.config.ts"));
    }

    /// <summary>
    /// ClientApp/validate-pages.mjs — copy as-is.
    /// </summary>
    public string ValidatePages()
    {
        return File.ReadAllText(Path.Combine(_templateHostDir, "ClientApp", "validate-pages.mjs"));
    }

    /// <summary>
    /// ClientApp/package.json — rename package.
    /// </summary>
    public string ClientAppPackageJson(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "ClientApp", "package.json");
        var content = File.ReadAllText(path);
        return content.Replace("@simplemodule/app", $"@{projectName.ToLowerInvariant()}/app", StringComparison.Ordinal);
    }

    /// <summary>
    /// Styles/app.css — clean up for new project (remove _scan, adjust module source paths).
    /// </summary>
    public string AppCss()
    {
        return """
            @import "tailwindcss";
            @import "@simplemodule/theme-default/theme.css";
            @source "../../modules/**/Components/**/*.razor";
            @source "../../modules/**/Views/**/*.tsx";
            @source "../../modules/**/Pages/**/*.tsx";
            """;
    }

    /// <summary>
    /// appsettings.json — copy as-is.
    /// </summary>
    public string AppSettings()
    {
        return File.ReadAllText(Path.Combine(_templateHostDir, "appsettings.json"));
    }

    /// <summary>
    /// appsettings.Development.json — copy as-is.
    /// </summary>
    public string AppSettingsDevelopment()
    {
        return File.ReadAllText(Path.Combine(_templateHostDir, "appsettings.Development.json"));
    }

    /// <summary>
    /// Properties/launchSettings.json — rename project.
    /// </summary>
    public string LaunchSettings(string projectName)
    {
        var path = Path.Combine(_templateHostDir, "Properties", "launchSettings.json");
        var content = File.ReadAllText(path);
        return content.Replace("SimpleModule", projectName, StringComparison.Ordinal);
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build cli/SimpleModule.Cli/SimpleModule.Cli.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```
feat(cli): add HostTemplates for project scaffolding
```

---

### Task 2: Add root config file templates to `ProjectTemplates`

**Files:**
- Modify: `cli/SimpleModule.Cli/Templates/ProjectTemplates.cs`

Add methods for the new root config files that the current implementation doesn't generate: `package.json`, `biome.json`, `tsconfig.json`, `.editorconfig`.

- [ ] **Step 1: Add root config template methods to `ProjectTemplates.cs`**

Add these methods after the existing `GlobalJson()` method:

```csharp
public string RootPackageJson(string projectName)
{
    var projectNameLower = projectName.ToLowerInvariant();
    if (_solution is null)
    {
        return FallbackRootPackageJson(projectNameLower);
    }

    var path = Path.Combine(_solution.RootPath, "package.json");
    if (!File.Exists(path))
    {
        return FallbackRootPackageJson(projectNameLower);
    }

    // Generate a clean workspace package.json for the new project
    return FallbackRootPackageJson(projectNameLower);
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
    // Adjust file includes for new project structure
    content = content.Replace(
        "\"modules/**\", \"packages/**\", \"template/**\", \"tests/**\", \"!**/wwwroot\"",
        "\"src/**\", \"tests/**\", \"!**/wwwroot\"",
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
    // Remove SimpleModule-specific excludes
    content = content.Replace(
        ", \"src/SimpleModule.UI/registry/templates\"",
        "",
        StringComparison.Ordinal
    );
    return content;
}

public string EditorConfig()
{
    if (_solution is null)
    {
        return ""; // .editorconfig is optional
    }

    var path = Path.Combine(_solution.RootPath, ".editorconfig");
    return File.Exists(path) ? File.ReadAllText(path) : "";
}
```

And the fallback methods:

```csharp
private static string FallbackRootPackageJson(string projectNameLower) =>
    $$"""
        {
          "private": true,
          "name": "{{projectNameLower}}",
          "version": "0.0.0",
          "workspaces": [
            "src/modules/*/src/*",
            "src/{{projectNameLower}}.Host/ClientApp"
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
            "tailwindcss": "^4.2.1"
          }
        }
        """;

private static string FallbackBiomeJson() =>
    """
        {
          "$schema": "https://biomejs.dev/schemas/2.4.7/schema.json",
          "vcs": { "enabled": true, "clientKind": "git", "useIgnoreFile": true },
          "formatter": { "enabled": true, "indentStyle": "space", "indentWidth": 2, "lineWidth": 100 },
          "css": { "parser": { "tailwindDirectives": true } },
          "javascript": { "formatter": { "quoteStyle": "single", "trailingCommas": "all", "semicolons": "always" } },
          "linter": { "enabled": true, "rules": { "recommended": true } },
          "files": { "includes": ["src/**", "tests/**", "!**/wwwroot"] }
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
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build cli/SimpleModule.Cli/SimpleModule.Cli.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```
feat(cli): add root config file templates for new project scaffolding
```

---

### Task 3: Update `SolutionContext` to support new project structure

**Files:**
- Modify: `cli/SimpleModule.Cli/Infrastructure/SolutionContext.cs`

The current `SolutionContext` hardcodes `SimpleModule.Host` as the API project path. The new project uses `{ProjectName}.Host`. We need to make `ApiCsprojPath` discoverable.

- [ ] **Step 1: Update `SolutionContext` to discover the Host .csproj dynamically**

Replace the hardcoded `ApiCsprojPath` assignment in the constructor:

```csharp
private SolutionContext(string rootPath, string slnxPath)
{
    RootPath = rootPath;
    SlnxPath = slnxPath;
    ModulesPath = Path.Combine(rootPath, "src", "modules");

    // Discover the Host/API project — look for *.Host.csproj or *.Api.csproj in src/
    ApiCsprojPath = DiscoverApiCsproj(rootPath);

    ExistingModules = Directory.Exists(ModulesPath)
        ? Directory
            .GetDirectories(ModulesPath)
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .Cast<string>()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList()
        : [];
}

private static string DiscoverApiCsproj(string rootPath)
{
    var srcDir = Path.Combine(rootPath, "src");

    if (Directory.Exists(srcDir))
    {
        // Look for *.Host.csproj first (new convention)
        foreach (var dir in Directory.GetDirectories(srcDir))
        {
            var dirName = Path.GetFileName(dir);
            if (dirName is not null && dirName.EndsWith(".Host", StringComparison.Ordinal))
            {
                var csproj = Path.Combine(dir, $"{dirName}.csproj");
                if (File.Exists(csproj))
                {
                    return csproj;
                }
            }
        }

        // Fall back to *.Api.csproj
        foreach (var dir in Directory.GetDirectories(srcDir))
        {
            var dirName = Path.GetFileName(dir);
            if (dirName is not null && dirName.EndsWith(".Api", StringComparison.Ordinal))
            {
                var csproj = Path.Combine(dir, $"{dirName}.csproj");
                if (File.Exists(csproj))
                {
                    return csproj;
                }
            }
        }
    }

    // Look in template/ for the original SimpleModule.Host
    var templateHost = Path.Combine(rootPath, "template", "SimpleModule.Host", "SimpleModule.Host.csproj");
    if (File.Exists(templateHost))
    {
        return templateHost;
    }

    // Absolute fallback
    return Path.Combine(rootPath, "src", "SimpleModule.Host", "SimpleModule.Host.csproj");
}
```

- [ ] **Step 2: Build and run existing tests to verify no regression**

Run: `dotnet build cli/SimpleModule.Cli/SimpleModule.Cli.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```
refactor(cli): make SolutionContext discover Host/Api csproj dynamically
```

---

### Task 4: Rewrite `NewProjectCommand` to scaffold from template

**Files:**
- Modify: `cli/SimpleModule.Cli/Commands/New/NewProjectCommand.cs`

This is the main task. Replace the current bare-skeleton scaffolding with the full template mirroring.

- [ ] **Step 1: Rewrite `NewProjectCommand.Execute()`**

```csharp
using SimpleModule.Cli.Infrastructure;
using SimpleModule.Cli.Templates;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewProjectCommand : Command<NewProjectSettings>
{
    private const string StarterModuleName = "Home";

    public override int Execute(CommandContext context, NewProjectSettings settings)
    {
        var projectName = settings.ResolveName();
        var outputDir = settings.ResolveOutputDir();
        var rootDir = Path.Combine(outputDir, projectName);

        if (!settings.DryRun && Directory.Exists(rootDir) && Directory.GetFileSystemEntries(rootDir).Length > 0)
        {
            AnsiConsole.MarkupLine($"[red]Directory '{Markup.Escape(rootDir)}' already exists and is not empty.[/]");
            return 1;
        }

        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine("[red]No .slnx file found. Run this command from inside the SimpleModule repository.[/]");
            return 1;
        }

        var templates = new ProjectTemplates(solution);
        var hostTemplates = new HostTemplates(solution);

        var hostDir = Path.Combine(rootDir, "src", $"{projectName}.Host");
        var modulesDir = Path.Combine(rootDir, "src", "modules");
        var testsSharedDir = Path.Combine(rootDir, "tests", $"{projectName}.Tests.Shared");

        // Calculate relative path from host project to the framework directory
        // New project is at: {outputDir}/{projectName}/src/{projectName}.Host/
        // Framework is at: {solution.RootPath}/framework/
        var frameworkRelativePath = Path.GetRelativePath(hostDir, Path.Combine(solution.RootPath, "framework"));

        var ops = new List<(string Path, FileAction Action)>();

        void Plan(string path) => ops.Add((path, FileAction.Create));

        // Root config files
        Plan(Path.Combine(rootDir, $"{projectName}.slnx"));
        Plan(Path.Combine(rootDir, "Directory.Build.props"));
        Plan(Path.Combine(rootDir, "Directory.Packages.props"));
        Plan(Path.Combine(rootDir, "global.json"));
        Plan(Path.Combine(rootDir, "package.json"));
        Plan(Path.Combine(rootDir, "biome.json"));
        Plan(Path.Combine(rootDir, "tsconfig.json"));

        // Host project files
        Plan(Path.Combine(hostDir, $"{projectName}.Host.csproj"));
        Plan(Path.Combine(hostDir, "Program.cs"));
        Plan(Path.Combine(hostDir, "appsettings.json"));
        Plan(Path.Combine(hostDir, "appsettings.Development.json"));
        Plan(Path.Combine(hostDir, "Properties", "launchSettings.json"));
        Plan(Path.Combine(hostDir, "Components", "App.razor"));
        Plan(Path.Combine(hostDir, "Components", "InertiaShell.razor"));
        Plan(Path.Combine(hostDir, "Components", "Routes.razor"));
        Plan(Path.Combine(hostDir, "Components", "_Imports.razor"));
        Plan(Path.Combine(hostDir, "ClientApp", "app.tsx"));
        Plan(Path.Combine(hostDir, "ClientApp", "vite.config.ts"));
        Plan(Path.Combine(hostDir, "ClientApp", "validate-pages.mjs"));
        Plan(Path.Combine(hostDir, "ClientApp", "package.json"));
        Plan(Path.Combine(hostDir, "Styles", "app.css"));

        // Tests.Shared
        Plan(Path.Combine(testsSharedDir, $"{projectName}.Tests.Shared.csproj"));

        if (settings.DryRun)
        {
            RenderDryRunTree(projectName, ops);
            return 0;
        }

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"Creating project '{projectName}'...", ctx =>
            {
                // Create directories
                Directory.CreateDirectory(Path.Combine(hostDir, "Components"));
                Directory.CreateDirectory(Path.Combine(hostDir, "ClientApp"));
                Directory.CreateDirectory(Path.Combine(hostDir, "Styles"));
                Directory.CreateDirectory(Path.Combine(hostDir, "Properties"));
                Directory.CreateDirectory(Path.Combine(hostDir, "wwwroot"));
                Directory.CreateDirectory(modulesDir);
                Directory.CreateDirectory(testsSharedDir);

                // Root config files
                ctx.Status("Writing root config files...");
                File.WriteAllText(Path.Combine(rootDir, $"{projectName}.slnx"), GenerateSlnx(projectName));
                File.WriteAllText(Path.Combine(rootDir, "Directory.Build.props"), templates.DirectoryBuildProps());
                File.WriteAllText(Path.Combine(rootDir, "Directory.Packages.props"), templates.DirectoryPackagesProps());
                File.WriteAllText(Path.Combine(rootDir, "global.json"), templates.GlobalJson());
                File.WriteAllText(Path.Combine(rootDir, "package.json"), templates.RootPackageJson(projectName));
                File.WriteAllText(Path.Combine(rootDir, "biome.json"), templates.BiomeJson());
                File.WriteAllText(Path.Combine(rootDir, "tsconfig.json"), templates.TsconfigJson());

                var editorConfig = templates.EditorConfig();
                if (!string.IsNullOrEmpty(editorConfig))
                {
                    File.WriteAllText(Path.Combine(rootDir, ".editorconfig"), editorConfig);
                }

                // Host project files
                ctx.Status("Writing host project...");
                File.WriteAllText(Path.Combine(hostDir, $"{projectName}.Host.csproj"), hostTemplates.HostCsproj(projectName, frameworkRelativePath));
                File.WriteAllText(Path.Combine(hostDir, "Program.cs"), hostTemplates.ProgramCs(projectName));
                File.WriteAllText(Path.Combine(hostDir, "appsettings.json"), hostTemplates.AppSettings());
                File.WriteAllText(Path.Combine(hostDir, "appsettings.Development.json"), hostTemplates.AppSettingsDevelopment());
                File.WriteAllText(Path.Combine(hostDir, "Properties", "launchSettings.json"), hostTemplates.LaunchSettings(projectName));

                // Blazor components
                File.WriteAllText(Path.Combine(hostDir, "Components", "App.razor"), hostTemplates.AppRazor(projectName));
                File.WriteAllText(Path.Combine(hostDir, "Components", "InertiaShell.razor"), hostTemplates.InertiaShellRazor(projectName));
                File.WriteAllText(Path.Combine(hostDir, "Components", "Routes.razor"), hostTemplates.RoutesRazor(projectName));
                File.WriteAllText(Path.Combine(hostDir, "Components", "_Imports.razor"), hostTemplates.ImportsRazor(projectName));

                // ClientApp
                File.WriteAllText(Path.Combine(hostDir, "ClientApp", "app.tsx"), hostTemplates.AppTsx());
                File.WriteAllText(Path.Combine(hostDir, "ClientApp", "vite.config.ts"), hostTemplates.ViteConfig());
                File.WriteAllText(Path.Combine(hostDir, "ClientApp", "validate-pages.mjs"), hostTemplates.ValidatePages());
                File.WriteAllText(Path.Combine(hostDir, "ClientApp", "package.json"), hostTemplates.ClientAppPackageJson(projectName));

                // Styles
                File.WriteAllText(Path.Combine(hostDir, "Styles", "app.css"), hostTemplates.AppCss());

                // Tests.Shared
                File.WriteAllText(Path.Combine(testsSharedDir, $"{projectName}.Tests.Shared.csproj"), templates.TestsSharedCsproj(projectName));

                // Create the starter module
                ctx.Status($"Creating '{StarterModuleName}' module...");
                CreateStarterModule(solution, rootDir, projectName);
            });

        RenderCreatedTree(projectName, ops);

        AnsiConsole.MarkupLine($"\n[green]Project '{Markup.Escape(projectName)}' created![/]");
        AnsiConsole.MarkupLine("[dim]Next steps:[/]");
        AnsiConsole.MarkupLine($"[dim]  cd {Markup.Escape(projectName)}[/]");
        AnsiConsole.MarkupLine("[dim]  npm install[/]");
        AnsiConsole.MarkupLine("[dim]  npm run build[/]");
        AnsiConsole.MarkupLine("[dim]  dotnet build[/]");
        AnsiConsole.MarkupLine($"[dim]  dotnet run --project src/{Markup.Escape(projectName)}.Host[/]");
        return 0;
    }

    private static void CreateStarterModule(SolutionContext repoSolution, string rootDir, string projectName)
    {
        const string moduleName = StarterModuleName;
        var singularName = ModuleTemplates.GetSingularName(moduleName);
        var templates = new ModuleTemplates(repoSolution);

        var modulesDir = Path.Combine(rootDir, "src", "modules");
        var contractsDir = Path.Combine(modulesDir, moduleName, "src", $"{moduleName}.Contracts");
        var moduleDir = Path.Combine(modulesDir, moduleName, "src", moduleName);
        var eventsDir = Path.Combine(contractsDir, "Events");
        var endpointsDir = Path.Combine(moduleDir, "Endpoints", moduleName);
        var pagesDir = Path.Combine(moduleDir, "Pages");
        var testDir = Path.Combine(modulesDir, moduleName, "tests", $"{moduleName}.Tests");

        Directory.CreateDirectory(eventsDir);
        Directory.CreateDirectory(endpointsDir);
        Directory.CreateDirectory(pagesDir);
        Directory.CreateDirectory(Path.Combine(testDir, "Unit"));
        Directory.CreateDirectory(Path.Combine(testDir, "Integration"));

        // Contracts
        File.WriteAllText(Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"), templates.ContractsCsproj(moduleName));
        File.WriteAllText(Path.Combine(contractsDir, $"I{singularName}Contracts.cs"), templates.ContractsInterface(moduleName, singularName));
        File.WriteAllText(Path.Combine(contractsDir, $"{singularName}.cs"), templates.DtoClass(moduleName, singularName));
        File.WriteAllText(Path.Combine(eventsDir, $"{singularName}CreatedEvent.cs"), templates.EventClass(moduleName, singularName));

        // Module implementation
        File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}.csproj"), templates.ModuleCsproj(moduleName));
        File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}Module.cs"), templates.ModuleClass(moduleName, singularName));
        File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}Constants.cs"), templates.ConstantsClass(moduleName, singularName));
        File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}DbContext.cs"), templates.DbContextClass(moduleName, singularName));
        File.WriteAllText(Path.Combine(moduleDir, $"{singularName}Service.cs"), templates.ServiceClass(moduleName, singularName));
        File.WriteAllText(Path.Combine(endpointsDir, "GetAllEndpoint.cs"), templates.GetAllEndpoint(moduleName, singularName));

        // Pages/index.ts for the module
        File.WriteAllText(Path.Combine(pagesDir, "index.ts"), $$"""
            export const pages: Record<string, any> = {};
            """);

        // Tests
        File.WriteAllText(Path.Combine(testDir, $"{moduleName}.Tests.csproj"), templates.TestCsproj(moduleName));
        File.WriteAllText(Path.Combine(testDir, "GlobalUsings.cs"), templates.GlobalUsings());
        File.WriteAllText(Path.Combine(testDir, "Unit", $"{singularName}ServiceTests.cs"), templates.UnitTestSkeleton(moduleName, singularName));
        File.WriteAllText(Path.Combine(testDir, "Integration", $"{moduleName}EndpointTests.cs"), templates.IntegrationTestSkeleton(moduleName, singularName));

        // Register module in Host .csproj and .slnx
        var slnxPath = Path.Combine(rootDir, $"{projectName}.slnx");
        var hostCsprojPath = Path.Combine(rootDir, "src", $"{projectName}.Host", $"{projectName}.Host.csproj");

        SlnxManipulator.AddModuleEntries(slnxPath, moduleName);
        ProjectManipulator.AddProjectReference(
            hostCsprojPath,
            $@"..\modules\{moduleName}\src\{moduleName}\{moduleName}.csproj"
        );
    }

    private static string GenerateSlnx(string projectName) =>
        $"""
            <Solution>
                <Configurations>
                    <Platform Name="Any CPU" />
                    <Platform Name="x64" />
                    <Platform Name="x86" />
                </Configurations>
                <Folder Name="/src/">
                    <Project Path="src/{projectName}.Host/{projectName}.Host.csproj" />
                </Folder>
                <Folder Name="/modules/" />
                <Folder Name="/tests/">
                    <Project Path="tests/{projectName}.Tests.Shared/{projectName}.Tests.Shared.csproj" />
                </Folder>
            </Solution>
            """;

    private static void RenderDryRunTree(string projectName, List<(string Path, FileAction Action)> ops)
    {
        AnsiConsole.MarkupLine("[dim]Dry run — no files written[/]\n");
        var tree = new Tree($"[blue]{Markup.Escape(projectName)}/[/]");
        foreach (var (path, _) in ops)
        {
            tree.AddNode($"[green]{Markup.Escape(Path.GetFileName(path))}[/]");
        }
        AnsiConsole.Write(tree);
    }

    private static void RenderCreatedTree(string projectName, List<(string Path, FileAction Action)> ops)
    {
        AnsiConsole.MarkupLine("");
        var tree = new Tree($"[blue]{Markup.Escape(projectName)}/[/]");
        foreach (var (path, action) in ops)
        {
            var label = action == FileAction.Modify
                ? $"[yellow]{Markup.Escape(Path.GetFileName(path))}[/] [dim](modified)[/]"
                : $"[green]{Markup.Escape(Path.GetFileName(path))}[/]";
            tree.AddNode(label);
        }
        AnsiConsole.Write(tree);
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build cli/SimpleModule.Cli/SimpleModule.Cli.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```
feat(cli): rewrite sm new project to scaffold from template with starter module
```

---

### Task 5: Update `TestsSharedCsproj` to reference Host instead of Api

**Files:**
- Modify: `cli/SimpleModule.Cli/Templates/ProjectTemplates.cs`

The `TestsSharedCsproj` fallback references `{ProjectName}.Api` but new projects use `{ProjectName}.Host`.

- [ ] **Step 1: Update the fallback template**

In `ProjectTemplates.cs`, update `FallbackTestsSharedCsproj`:

```csharp
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
```

Also update the `TestsSharedCsproj` method to handle both Api and Host naming:

In the non-fallback path, after calling `TransformCsproj`, also replace `.Api.` → `.Host.` if the new project uses Host convention:

```csharp
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
    return TemplateExtractor.TransformCsproj(path, BaseProjectName, projectName, stripPatterns);
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build cli/SimpleModule.Cli/SimpleModule.Cli.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```
fix(cli): update TestsSharedCsproj fallback to reference Host project
```

---

### Task 6: Manual testing — run `sm new project` and verify output

**Files:** None (testing only)

- [ ] **Step 1: Run a dry run**

Run: `dotnet run --project cli/SimpleModule.Cli -- new project TestApp --dry-run`
Expected: Tree output showing all planned files

- [ ] **Step 2: Run the actual scaffolding**

Run from a temp directory:

```bash
cd /tmp
dotnet run --project /path/to/cli/SimpleModule.Cli -- new project TestApp
```

Expected: Project created with success message and next steps

- [ ] **Step 3: Verify the output structure**

```bash
find /tmp/TestApp -type f | sort
```

Expected files:
- Root: `TestApp.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `package.json`, `biome.json`, `tsconfig.json`, `.editorconfig`
- Host: `src/TestApp.Host/TestApp.Host.csproj`, `Program.cs`, `appsettings.json`, etc.
- Components: `App.razor`, `InertiaShell.razor`, `Routes.razor`, `_Imports.razor`
- ClientApp: `app.tsx`, `vite.config.ts`, `validate-pages.mjs`, `package.json`
- Home module: contracts, implementation, endpoints, tests
- Tests.Shared: `TestApp.Tests.Shared.csproj`

- [ ] **Step 4: Verify the generated project builds**

```bash
cd /tmp/TestApp
dotnet build
```

Expected: Build succeeded (may have warnings about framework references if not in repo tree)

- [ ] **Step 5: Verify the Home module .slnx entry exists**

Check `TestApp.slnx` contains a `/modules/Home/` folder with three project entries.

- [ ] **Step 6: Verify the Host .csproj has Home module reference**

Check `src/TestApp.Host/TestApp.Host.csproj` contains `<ProjectReference Include="..\modules\Home\src\Home\Home.csproj" />`.

- [ ] **Step 7: Clean up and commit**

```bash
rm -rf /tmp/TestApp
```

No code commit needed — this was a verification task.

---

### Task 7: Remove old project structure references

**Files:**
- Modify: `cli/SimpleModule.Cli/Templates/ProjectTemplates.cs`

Clean up the old Api/Core/Database/Generator-specific methods and fallbacks that are no longer used by `NewProjectCommand`. Keep them only if other commands use them.

- [ ] **Step 1: Check if any other command references the old methods**

Search for `ApiCsproj`, `CoreCsproj`, `DatabaseCsproj`, `GeneratorCsproj`, `ApiProgram` usage outside `NewProjectCommand`.

If they're only used by `NewProjectCommand`, remove them. If other code references them, keep them.

- [ ] **Step 2: Remove unused methods if confirmed**

Remove: `ApiCsproj()`, `ApiProgram()`, `CoreCsproj()`, `DatabaseCsproj()`, `GeneratorCsproj()`, and their fallbacks.

Also update `FallbackSlnx()` to match the new Host-based structure:

```csharp
private static string FallbackSlnx(string projectName) =>
    $"""
        <Solution>
            <Configurations>
                <Platform Name="Any CPU" />
                <Platform Name="x64" />
                <Platform Name="x86" />
            </Configurations>
            <Folder Name="/src/">
                <Project Path="src/{projectName}.Host/{projectName}.Host.csproj" />
            </Folder>
            <Folder Name="/modules/" />
            <Folder Name="/tests/">
                <Project Path="tests/{projectName}.Tests.Shared/{projectName}.Tests.Shared.csproj" />
            </Folder>
        </Solution>
        """;
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build cli/SimpleModule.Cli/SimpleModule.Cli.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```
refactor(cli): remove unused Api/Core/Database/Generator template methods
```
