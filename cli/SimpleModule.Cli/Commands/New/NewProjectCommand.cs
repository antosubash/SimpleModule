using SimpleModule.Cli.Infrastructure;
using SimpleModule.Cli.Templates;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewProjectCommand : Command<NewProjectSettings>
{
    public override int Execute(CommandContext context, NewProjectSettings settings)
    {
        var solution = SolutionContext.Discover();

        var projectName = settings.ResolveName();
        var outputDir = settings.ResolveOutputDir();
        var rootDir = Path.Combine(outputDir, projectName);

        if (
            !settings.DryRun
            && Directory.Exists(rootDir)
            && Directory.GetFileSystemEntries(rootDir).Length > 0
        )
        {
            AnsiConsole.MarkupLine(
                $"[red]Directory '{Markup.Escape(rootDir)}' already exists and is not empty.[/]"
            );
            return 1;
        }

        var frameworkVersion = NuGetVersionResolver.ResolveVersion(
            settings.FrameworkVersion,
            solution
        );

        if (settings.DryRun)
        {
            var ops = PlanFiles(projectName, rootDir);
            RenderDryRunTree(projectName, ops, rootDir);
            return 0;
        }

        AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .Start(
                $"Creating project '{projectName}'...",
                ctx =>
                {
                    ctx.Status("Scaffolding project files...");
                    ScaffoldProject(projectName, rootDir, solution, frameworkVersion);
                }
            );

        var allOps = PlanFiles(projectName, rootDir);
        RenderFileTree(projectName, allOps, rootDir);

        AnsiConsole.MarkupLine($"\n[green]Project '{Markup.Escape(projectName)}' created![/]");
        AnsiConsole.MarkupLine("[dim]Next steps:[/]");
        AnsiConsole.MarkupLine($"[dim]  cd {Markup.Escape(projectName)}[/]");
        AnsiConsole.MarkupLine("[dim]  npm install[/]");
        AnsiConsole.MarkupLine("[dim]  npm run build[/]");
        AnsiConsole.MarkupLine("[dim]  dotnet build[/]");
        AnsiConsole.MarkupLine(
            $"[dim]  dotnet run --project src/{Markup.Escape(projectName)}.Host[/]"
        );
        return 0;
    }

    public static void ScaffoldProject(
        string projectName,
        string rootDir,
        SolutionContext? solution,
        string frameworkVersion
    )
    {
        var projectTemplates = new ProjectTemplates(solution, frameworkVersion);
        var moduleTemplates = new ModuleTemplates(solution);

        const string moduleName = "Items";
        var singularName = ModuleTemplates.GetSingularName(moduleName);

        var hostDir = Path.Combine(rootDir, "src", $"{projectName}.Host");
        var modulesDir = Path.Combine(rootDir, "src", "modules");
        var testsSharedDir = Path.Combine(rootDir, "tests", $"{projectName}.Tests.Shared");

        var contractsDir = Path.Combine(modulesDir, moduleName, "src", $"{moduleName}.Contracts");
        var moduleDir = Path.Combine(modulesDir, moduleName, "src", moduleName);
        var eventsDir = Path.Combine(contractsDir, "Events");
        var endpointsDir = Path.Combine(moduleDir, "Endpoints", moduleName);
        var moduleTestDir = Path.Combine(modulesDir, moduleName, "tests", $"{moduleName}.Tests");

        // ── Create directories ────────────────────────────────
        Directory.CreateDirectory(Path.Combine(hostDir, "Components"));
        Directory.CreateDirectory(Path.Combine(hostDir, "ClientApp"));
        Directory.CreateDirectory(Path.Combine(hostDir, "Styles"));
        Directory.CreateDirectory(Path.Combine(hostDir, "Properties"));
        Directory.CreateDirectory(Path.Combine(hostDir, "wwwroot", "css"));
        Directory.CreateDirectory(eventsDir);
        Directory.CreateDirectory(endpointsDir);
        Directory.CreateDirectory(Path.Combine(moduleDir, "Pages"));
        Directory.CreateDirectory(Path.Combine(moduleDir, "Views"));
        Directory.CreateDirectory(Path.Combine(moduleTestDir, "Unit"));
        Directory.CreateDirectory(Path.Combine(moduleTestDir, "Integration"));
        Directory.CreateDirectory(testsSharedDir);

        // ── Root config files ─────────────────────────────────
        File.WriteAllText(Path.Combine(rootDir, $"{projectName}.slnx"), GenerateSlnx(projectName));
        File.WriteAllText(
            Path.Combine(rootDir, "Directory.Build.props"),
            projectTemplates.DirectoryBuildProps()
        );
        File.WriteAllText(
            Path.Combine(rootDir, "Directory.Packages.props"),
            projectTemplates.DirectoryPackagesProps()
        );
        File.WriteAllText(Path.Combine(rootDir, "global.json"), projectTemplates.GlobalJson());
        File.WriteAllText(
            Path.Combine(rootDir, "package.json"),
            projectTemplates.RootPackageJson(
                projectName,
                solution is not null ? Path.Combine(solution.RootPath, "packages") : null
            )
        );
        File.WriteAllText(Path.Combine(rootDir, "biome.json"), projectTemplates.BiomeJson());
        File.WriteAllText(Path.Combine(rootDir, "tsconfig.json"), projectTemplates.TsconfigJson());
        var editorConfig = projectTemplates.EditorConfig();
        if (!string.IsNullOrEmpty(editorConfig))
        {
            File.WriteAllText(Path.Combine(rootDir, ".editorconfig"), editorConfig);
        }

        File.WriteAllText(
            Path.Combine(rootDir, "nuget.config"),
            ProjectTemplates.NugetConfig(solution?.RootPath)
        );

        // ── Host project ──────────────────────────────────────
        File.WriteAllText(
            Path.Combine(hostDir, $"{projectName}.Host.csproj"),
            HostTemplates.HostCsproj(projectName)
        );
        File.WriteAllText(Path.Combine(hostDir, "Program.cs"), HostTemplates.ProgramCs());
        File.WriteAllText(
            Path.Combine(hostDir, "Components", "App.razor"),
            HostTemplates.AppRazor(projectName)
        );
        File.WriteAllText(
            Path.Combine(hostDir, "Components", "InertiaShell.razor"),
            HostTemplates.InertiaShellRazor(projectName)
        );
        File.WriteAllText(
            Path.Combine(hostDir, "Components", "Routes.razor"),
            HostTemplates.RoutesRazor()
        );
        File.WriteAllText(
            Path.Combine(hostDir, "Components", "_Imports.razor"),
            HostTemplates.ImportsRazor(projectName)
        );
        File.WriteAllText(Path.Combine(hostDir, "ClientApp", "app.tsx"), HostTemplates.AppTsx());
        File.WriteAllText(
            Path.Combine(hostDir, "ClientApp", "vite.config.ts"),
            HostTemplates.ViteConfig()
        );
        File.WriteAllText(
            Path.Combine(hostDir, "ClientApp", "validate-pages.mjs"),
            HostTemplates.ValidatePages()
        );
        File.WriteAllText(
            Path.Combine(hostDir, "ClientApp", "package.json"),
            HostTemplates.ClientAppPackageJson(projectName)
        );
        File.WriteAllText(Path.Combine(hostDir, "Styles", "app.css"), HostTemplates.AppCss());
        File.WriteAllText(Path.Combine(hostDir, "wwwroot", "css", "app.css"), MinimalAppCss());
        File.WriteAllText(Path.Combine(hostDir, "appsettings.json"), HostTemplates.AppSettings());
        File.WriteAllText(
            Path.Combine(hostDir, "appsettings.Development.json"),
            HostTemplates.AppSettingsDevelopment()
        );
        File.WriteAllText(
            Path.Combine(hostDir, "Properties", "launchSettings.json"),
            HostTemplates.LaunchSettings(projectName)
        );

        // ── Home module ───────────────────────────────────────
        File.WriteAllText(
            Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"),
            moduleTemplates.ContractsCsproj(moduleName)
        );
        File.WriteAllText(
            Path.Combine(contractsDir, $"I{singularName}Contracts.cs"),
            moduleTemplates.ContractsInterface(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(contractsDir, $"{singularName}.cs"),
            moduleTemplates.DtoClass(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(eventsDir, $"{singularName}CreatedEvent.cs"),
            moduleTemplates.EventClass(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(moduleDir, $"{moduleName}.csproj"),
            moduleTemplates.ModuleCsproj(moduleName)
        );
        File.WriteAllText(
            Path.Combine(moduleDir, $"{moduleName}Module.cs"),
            StarterModuleClass(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(moduleDir, $"{moduleName}Constants.cs"),
            moduleTemplates.ConstantsClass(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(moduleDir, $"{moduleName}DbContext.cs"),
            moduleTemplates.DbContextClass(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(moduleDir, $"{singularName}Service.cs"),
            moduleTemplates.ServiceClass(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(endpointsDir, "GetAllEndpoint.cs"),
            moduleTemplates.GetAllEndpoint(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(moduleDir, "Views", "WelcomeEndpoint.cs"),
            StarterWelcomeEndpoint(moduleName)
        );
        File.WriteAllText(
            Path.Combine(moduleDir, "Pages", "index.ts"),
            $$"""
            export const pages: Record<string, any> = {
              '{{moduleName}}/Welcome': () => import('./Welcome'),
            };
            """
        );
        File.WriteAllText(
            Path.Combine(moduleDir, "Pages", "Welcome.tsx"),
            StarterWelcomePage(projectName)
        );
        File.WriteAllText(Path.Combine(moduleDir, "vite.config.ts"), StarterViteConfig());
        File.WriteAllText(Path.Combine(moduleDir, "package.json"), StarterPackageJson(projectName));
        File.WriteAllText(
            Path.Combine(moduleTestDir, $"{moduleName}.Tests.csproj"),
            moduleTemplates.TestCsproj(moduleName)
        );
        File.WriteAllText(
            Path.Combine(moduleTestDir, "GlobalUsings.cs"),
            moduleTemplates.GlobalUsings()
        );
        File.WriteAllText(
            Path.Combine(moduleTestDir, "Unit", $"{singularName}ServiceTests.cs"),
            moduleTemplates.UnitTestSkeleton(moduleName, singularName)
        );
        File.WriteAllText(
            Path.Combine(moduleTestDir, "Integration", $"{moduleName}EndpointTests.cs"),
            moduleTemplates.IntegrationTestSkeleton(moduleName, singularName)
        );

        // ── Tests.Shared ──────────────────────────────────────
        File.WriteAllText(
            Path.Combine(testsSharedDir, $"{projectName}.Tests.Shared.csproj"),
            projectTemplates.TestsSharedCsproj(projectName)
        );

        // ── Wire up solution ──────────────────────────────────
        var slnxPath = Path.Combine(rootDir, $"{projectName}.slnx");
        SlnxManipulator.AddModuleEntries(slnxPath, moduleName);

        var hostCsprojPath = Path.Combine(hostDir, $"{projectName}.Host.csproj");
        ProjectManipulator.AddProjectReference(
            hostCsprojPath,
            $@"..\modules\{moduleName}\src\{moduleName}\{moduleName}.csproj"
        );
    }

    private static List<(string Path, FileAction Action)> PlanFiles(
        string projectName,
        string rootDir
    )
    {
        var ops = new List<(string Path, FileAction Action)>();
        void Plan(string path) => ops.Add((path, FileAction.Create));

        var hostDir = Path.Combine(rootDir, "src", $"{projectName}.Host");
        var modulesDir = Path.Combine(rootDir, "src", "modules");
        var testsSharedDir = Path.Combine(rootDir, "tests", $"{projectName}.Tests.Shared");

        const string moduleName = "Items";
        var singularName = ModuleTemplates.GetSingularName(moduleName);

        // Root config files
        Plan(Path.Combine(rootDir, $"{projectName}.slnx"));
        Plan(Path.Combine(rootDir, "Directory.Build.props"));
        Plan(Path.Combine(rootDir, "Directory.Packages.props"));
        Plan(Path.Combine(rootDir, "global.json"));
        Plan(Path.Combine(rootDir, "package.json"));
        Plan(Path.Combine(rootDir, "biome.json"));
        Plan(Path.Combine(rootDir, "tsconfig.json"));
        Plan(Path.Combine(rootDir, ".editorconfig"));
        Plan(Path.Combine(rootDir, "nuget.config"));

        // Host project files
        Plan(Path.Combine(hostDir, $"{projectName}.Host.csproj"));
        Plan(Path.Combine(hostDir, "Program.cs"));
        Plan(Path.Combine(hostDir, "Components", "App.razor"));
        Plan(Path.Combine(hostDir, "Components", "InertiaShell.razor"));
        Plan(Path.Combine(hostDir, "Components", "Routes.razor"));
        Plan(Path.Combine(hostDir, "Components", "_Imports.razor"));
        Plan(Path.Combine(hostDir, "ClientApp", "app.tsx"));
        Plan(Path.Combine(hostDir, "ClientApp", "vite.config.ts"));
        Plan(Path.Combine(hostDir, "ClientApp", "validate-pages.mjs"));
        Plan(Path.Combine(hostDir, "ClientApp", "package.json"));
        Plan(Path.Combine(hostDir, "Styles", "app.css"));
        Plan(Path.Combine(hostDir, "wwwroot", "css", "app.css"));
        Plan(Path.Combine(hostDir, "appsettings.json"));
        Plan(Path.Combine(hostDir, "appsettings.Development.json"));
        Plan(Path.Combine(hostDir, "Properties", "launchSettings.json"));

        // Home module files
        var contractsDir = Path.Combine(modulesDir, moduleName, "src", $"{moduleName}.Contracts");
        var moduleDir = Path.Combine(modulesDir, moduleName, "src", moduleName);
        var eventsDir = Path.Combine(contractsDir, "Events");
        var endpointsDir = Path.Combine(moduleDir, "Endpoints", moduleName);
        var moduleTestDir = Path.Combine(modulesDir, moduleName, "tests", $"{moduleName}.Tests");

        Plan(Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"));
        Plan(Path.Combine(contractsDir, $"I{singularName}Contracts.cs"));
        Plan(Path.Combine(contractsDir, $"{singularName}.cs"));
        Plan(Path.Combine(eventsDir, $"{singularName}CreatedEvent.cs"));
        Plan(Path.Combine(moduleDir, $"{moduleName}.csproj"));
        Plan(Path.Combine(moduleDir, $"{moduleName}Module.cs"));
        Plan(Path.Combine(moduleDir, $"{moduleName}Constants.cs"));
        Plan(Path.Combine(moduleDir, $"{moduleName}DbContext.cs"));
        Plan(Path.Combine(moduleDir, $"{singularName}Service.cs"));
        Plan(Path.Combine(endpointsDir, "GetAllEndpoint.cs"));
        Plan(Path.Combine(moduleDir, "Views", "WelcomeEndpoint.cs"));
        Plan(Path.Combine(moduleDir, "Pages", "index.ts"));
        Plan(Path.Combine(moduleDir, "Pages", "Welcome.tsx"));
        Plan(Path.Combine(moduleDir, "vite.config.ts"));
        Plan(Path.Combine(moduleDir, "package.json"));
        Plan(Path.Combine(moduleTestDir, $"{moduleName}.Tests.csproj"));
        Plan(Path.Combine(moduleTestDir, "GlobalUsings.cs"));
        Plan(Path.Combine(moduleTestDir, "Unit", $"{singularName}ServiceTests.cs"));
        Plan(Path.Combine(moduleTestDir, "Integration", $"{moduleName}EndpointTests.cs"));

        // Tests.Shared
        Plan(Path.Combine(testsSharedDir, $"{projectName}.Tests.Shared.csproj"));

        return ops;
    }

    private static string GenerateSlnx(string projectName)
    {
        return $"""
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
    }

    private static void RenderDryRunTree(
        string projectName,
        List<(string Path, FileAction Action)> ops,
        string rootDir
    )
    {
        AnsiConsole.MarkupLine("[dim]Dry run — no files written[/]\n");
        RenderFileTree(projectName, ops, rootDir, isDryRun: true);
    }

    private static string StarterModuleClass(string moduleName, string singularName) =>
        $$"""
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using SimpleModule.Core;
            using SimpleModule.Database;
            using SimpleModule.{{moduleName}}.Contracts;

            namespace SimpleModule.{{moduleName}};

            [Module({{moduleName}}Constants.ModuleName, RoutePrefix = {{moduleName}}Constants.RoutePrefix, ViewPrefix = "/")]
            public class {{moduleName}}Module : IModule
            {
                public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
                {
                    services.AddModuleDbContext<{{moduleName}}DbContext>(configuration, {{moduleName}}Constants.ModuleName);
                    services.AddScoped<I{{singularName}}Contracts, {{singularName}}Service>();
                }
            }
            """;

    private static string StarterWelcomeEndpoint(string moduleName) =>
        $$"""
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;
            using SimpleModule.Core.Inertia;

            namespace SimpleModule.{{moduleName}}.Views;

            public class WelcomeEndpoint : IViewEndpoint
            {
                public void Map(IEndpointRouteBuilder app)
                {
                    app.MapGet(
                            "/",
                            () => Inertia.Render("{{moduleName}}/Welcome", new { })
                        )
                        .ExcludeFromDescription()
                        .AllowAnonymous();
                }
            }
            """;

    private static string StarterWelcomePage(string projectName) =>
        """
            export default function Welcome() {
              return (
                <div style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  minHeight: 'calc(100vh - 8rem)',
                  fontFamily: "'DM Sans', system-ui, -apple-system, sans-serif",
                  background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                  color: 'white',
                  margin: '-2rem -1.5rem -4rem',
                  padding: '4rem 1.5rem',
                  borderRadius: '1.5rem',
                }}>
                  <div style={{ textAlign: 'center', maxWidth: '600px', padding: '2rem' }}>
                    <div style={{
                      width: '80px',
                      height: '80px',
                      borderRadius: '20px',
                      background: 'rgba(255,255,255,0.15)',
                      backdropFilter: 'blur(10px)',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      margin: '0 auto 1.5rem',
                      fontSize: '2rem',
                      fontWeight: 'bold',
                    }}>
                      ✦
                    </div>
                    <h1 style={{ fontSize: '3rem', fontWeight: 800, margin: '0 0 0.5rem', letterSpacing: '-0.02em' }}>
                      __PROJECT_NAME__
                    </h1>
                    <p style={{ fontSize: '1.125rem', opacity: 0.85, margin: '0 0 2.5rem', lineHeight: 1.7 }}>
                      Your modular monolith is running. Start building!
                    </p>
                    <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center', flexWrap: 'wrap' }}>
                      <a
                        href="/swagger"
                        style={{
                          padding: '0.75rem 1.75rem',
                          borderRadius: '12px',
                          background: 'rgba(255,255,255,0.2)',
                          backdropFilter: 'blur(10px)',
                          color: 'white',
                          textDecoration: 'none',
                          fontWeight: 600,
                          fontSize: '0.875rem',
                          border: '1px solid rgba(255,255,255,0.3)',
                          transition: 'all 0.2s',
                        }}
                      >
                        API Docs →
                      </a>
                      <a
                        href="/api/items"
                        style={{
                          padding: '0.75rem 1.75rem',
                          borderRadius: '12px',
                          background: 'rgba(255,255,255,0.1)',
                          backdropFilter: 'blur(10px)',
                          color: 'white',
                          textDecoration: 'none',
                          fontWeight: 600,
                          fontSize: '0.875rem',
                          border: '1px solid rgba(255,255,255,0.15)',
                          transition: 'all 0.2s',
                        }}
                      >
                        Items API →
                      </a>
                    </div>
                    <p style={{ fontSize: '0.8rem', opacity: 0.5, marginTop: '3rem' }}>
                      Run <code style={{ background: 'rgba(255,255,255,0.15)', padding: '0.15rem 0.5rem', borderRadius: '4px', fontSize: '0.75rem' }}>sm new module {'<name>'}</code> to add more modules
                    </p>
                  </div>
                </div>
              );
            }
            """.Replace("__PROJECT_NAME__", projectName, StringComparison.Ordinal);

    private static string StarterViteConfig() =>
        """
            import { defineModuleConfig } from '@simplemodule/client/module';

            export default defineModuleConfig(__dirname);
            """;

    private static string StarterPackageJson(string projectName) =>
        $$"""
            {
              "private": true,
              "name": "@{{projectName.ToLowerInvariant()}}/items",
              "version": "0.0.0",
              "scripts": {
                "build": "vite build",
                "build:dev": "cross-env VITE_MODE=dev vite build",
                "watch": "cross-env VITE_MODE=dev vite build --watch"
              },
              "peerDependencies": {
                "react": "^19.0.0",
                "react-dom": "^19.0.0"
              }
            }
            """;

    private static string MinimalAppCss() =>
        """
            /* ============================================================
               SimpleModule — Minimal Layout CSS
               Covers PublicLayout nav, dark mode, buttons, and bg-mesh.
               Run `npm run build` (Tailwind) for the full design system.
               ============================================================ */

            /* --- Theme Custom Properties (Light) --- */
            :root {
              --color-primary: #16a34a;
              --color-primary-hover: #15803d;
              --color-primary-subtle: rgba(22, 163, 74, 0.08);
              --color-accent: #166534;
              --color-surface: #ffffff;
              --color-surface-raised: #f8fafc;
              --color-surface-sunken: #f1f5f9;
              --color-surface-overlay: rgba(255, 255, 255, 0.8);
              --color-text: #0f172a;
              --color-text-secondary: #475569;
              --color-text-muted: #94a3b8;
              --color-border: #e2e8f0;
              --color-border-strong: #cbd5e1;
            }

            /* --- Theme Custom Properties (Dark) --- */
            .dark {
              --color-primary-subtle: rgba(22, 163, 74, 0.15);
              --color-surface: #0f172a;
              --color-surface-raised: #1e293b;
              --color-surface-sunken: #0b1120;
              --color-surface-overlay: rgba(15, 23, 42, 0.85);
              --color-text: #f1f5f9;
              --color-text-secondary: #94a3b8;
              --color-text-muted: #64748b;
              --color-border: #1e293b;
              --color-border-strong: #334155;
            }

            /* --- Base --- */
            *, *::before, *::after { box-sizing: border-box; margin: 0; }
            html { scroll-behavior: smooth; -webkit-font-smoothing: antialiased; }
            body {
              font-family: 'DM Sans', system-ui, -apple-system, sans-serif;
              background: var(--color-surface-sunken);
              color: var(--color-text);
              transition: color 0.3s, background-color 0.3s;
            }
            a { color: var(--color-primary); transition: color 0.15s; text-decoration: none; }
            a:hover { color: var(--color-primary-hover); }

            /* --- Layout Utilities --- */
            .flex { display: flex; }
            .hidden { display: none; }
            .block { display: block; }
            .inline-flex { display: inline-flex; }
            .items-center { align-items: center; }
            .justify-center { justify-content: center; }
            .justify-between { justify-content: space-between; }
            .ml-auto { margin-left: auto; }
            .mx-auto { margin-left: auto; margin-right: auto; }
            .gap-1 { gap: 0.25rem; }
            .gap-2 { gap: 0.5rem; }
            .gap-2\.5 { gap: 0.625rem; }
            .gap-3 { gap: 0.75rem; }
            .gap-6 { gap: 1.5rem; }
            .px-3 { padding-left: 0.75rem; padding-right: 0.75rem; }
            .px-3\.5 { padding-left: 0.875rem; padding-right: 0.875rem; }
            .px-4 { padding-left: 1rem; padding-right: 1rem; }
            .px-5 { padding-left: 1.25rem; padding-right: 1.25rem; }
            .px-6 { padding-left: 1.5rem; padding-right: 1.5rem; }
            .py-1\.5 { padding-top: 0.375rem; padding-bottom: 0.375rem; }
            .py-2 { padding-top: 0.5rem; padding-bottom: 0.5rem; }
            .py-2\.5 { padding-top: 0.625rem; padding-bottom: 0.625rem; }
            .py-3 { padding-top: 0.75rem; padding-bottom: 0.75rem; }
            .mt-8 { margin-top: 2rem; }
            .mb-16 { margin-bottom: 4rem; }
            .w-8 { width: 2rem; }
            .w-9 { width: 2.25rem; }
            .h-8 { height: 2rem; }
            .h-9 { height: 2.25rem; }
            .min-h-screen { min-height: 100vh; }
            .max-w-7xl { max-width: 80rem; }
            .shrink-0 { flex-shrink: 0; }
            .overflow-hidden { overflow: hidden; }

            /* --- Positioning --- */
            .sticky { position: sticky; }
            .top-0 { top: 0; }
            .z-50 { z-index: 50; }
            .relative { position: relative; }
            .absolute { position: absolute; }

            /* --- Typography --- */
            .text-xs { font-size: 0.75rem; line-height: 1rem; }
            .text-sm { font-size: 0.875rem; line-height: 1.25rem; }
            .text-base { font-size: 1rem; line-height: 1.5rem; }
            .text-white { color: #fff; }
            .font-bold { font-weight: 700; }
            .font-semibold { font-weight: 600; }
            .font-medium { font-weight: 500; }
            .no-underline { text-decoration: none; }

            /* --- Colors (theme-aware) --- */
            .text-text { color: var(--color-text); }
            .text-text-muted { color: var(--color-text-muted); }
            .text-text-secondary { color: var(--color-text-secondary); }
            .text-primary { color: var(--color-primary); }
            .bg-surface { background-color: var(--color-surface); }
            .bg-surface-overlay { background-color: var(--color-surface-overlay); }
            .bg-surface-raised { background-color: var(--color-surface-raised); }
            .bg-transparent { background-color: transparent; }
            .border-border { border-color: var(--color-border); }
            .border-b { border-bottom: 1px solid var(--color-border); }

            /* --- Rounded --- */
            .rounded-lg { border-radius: 0.5rem; }
            .rounded-xl { border-radius: 0.75rem; }
            .rounded-2xl { border-radius: 1rem; }

            /* --- Shadows --- */
            .shadow-md { box-shadow: 0 4px 6px -1px rgba(0,0,0,.1), 0 2px 4px -2px rgba(0,0,0,.1); }

            /* --- Transitions --- */
            .transition-colors { transition-property: color, background-color, border-color; transition-duration: 0.15s; }
            .transition-transform { transition-property: transform; transition-duration: 0.15s; }
            .transition-all { transition-property: all; transition-duration: 0.15s; }
            .duration-200 { transition-duration: 0.2s; }
            .cursor-pointer { cursor: pointer; }
            .border-none { border: none; }

            /* --- Hover --- */
            .hover\:text-primary:hover { color: var(--color-primary); }
            .hover\:text-text:hover { color: var(--color-text); }
            .hover\:bg-surface-raised:hover { background-color: var(--color-surface-raised); }
            .hover\:bg-surface-hover:hover { background-color: var(--color-surface-raised); }
            .hover\:bg-primary-subtle:hover { background-color: var(--color-primary-subtle); }
            .hover\:scale-105:hover { transform: scale(1.05); }
            .group:hover .group-hover\:scale-105 { transform: scale(1.05); }

            /* --- Responsive --- */
            @media (min-width: 640px) {
              .sm\:flex { display: flex; }
              .sm\:inline-flex { display: inline-flex; }
              .sm\:px-6 { padding-left: 1.5rem; padding-right: 1.5rem; }
            }

            /* --- Dark mode toggle icons --- */
            .dark\:hidden { display: revert; }
            .dark .dark\:hidden { display: none; }
            .dark\:block { display: none; }
            .dark .dark\:block { display: block; }
            .w-\[18px\] { width: 18px; }
            .h-\[18px\] { height: 18px; }

            /* --- Buttons --- */
            .btn-primary, .btn-ghost, .btn-secondary {
              display: inline-flex; align-items: center; justify-content: center;
              gap: 0.5rem; padding: 0.625rem 1.25rem; border-radius: 0.75rem;
              font-size: 0.875rem; font-weight: 600; cursor: pointer;
              text-decoration: none; transition: all 0.2s ease-out; border: none;
            }
            .btn-primary {
              color: #fff;
              background: linear-gradient(135deg, var(--color-primary), var(--color-accent));
              box-shadow: 0 4px 14px rgba(22, 163, 74, 0.35);
            }
            .btn-primary:hover { box-shadow: 0 6px 20px rgba(22, 163, 74, 0.5); transform: translateY(-1px); }
            .btn-ghost { background: transparent; color: var(--color-text-secondary); }
            .btn-ghost:hover { background: var(--color-primary-subtle); color: var(--color-primary); }
            .btn-sm { padding: 0.375rem 0.875rem; font-size: 0.75rem; border-radius: 0.5rem; }

            /* --- Animated Background Mesh --- */
            .bg-mesh {
              position: fixed; inset: 0; z-index: -1; overflow: hidden;
              background: var(--color-surface-sunken);
            }
            .bg-mesh::before, .bg-mesh::after {
              content: ""; position: absolute; border-radius: 50%;
              filter: blur(100px); opacity: 0.15;
              animation: mesh-float 20s ease-in-out infinite;
            }
            .bg-mesh::before { width: 600px; height: 600px; background: var(--color-primary); top: -10%; right: -10%; }
            .bg-mesh::after { width: 500px; height: 500px; background: var(--color-accent); bottom: -10%; left: -10%; animation-delay: -10s; animation-direction: reverse; }
            .dark .bg-mesh::before, .dark .bg-mesh::after { opacity: 0.08; }
            @keyframes mesh-float {
              0%, 100% { transform: translate(0, 0) scale(1); }
              33% { transform: translate(40px, -30px) scale(1.05); }
              66% { transform: translate(-20px, 20px) scale(0.95); }
            }

            /* --- Dropdown menus (PublicLayout hover menus) --- */
            .group-hover\:block { display: none; }
            .group:hover > .group-hover\:block { display: block; }
            .group-hover\/sub\:block { display: none; }
            .group\/sub:hover > .group-hover\/sub\:block { display: block; }
            .top-full { top: 100%; }
            .left-0 { left: 0; }
            .left-full { left: 100%; }
            .mt-1 { margin-top: 0.25rem; }
            .ml-0\.5 { margin-left: 0.125rem; }
            .py-1 { padding-top: 0.25rem; padding-bottom: 0.25rem; }
            .min-w-\[160px\] { min-width: 160px; }
            .shadow-lg { box-shadow: 0 10px 15px -3px rgba(0,0,0,.1), 0 4px 6px -4px rgba(0,0,0,.1); }
            """;

    private static void RenderFileTree(
        string projectName,
        List<(string Path, FileAction Action)> ops,
        string rootDir,
        bool isDryRun = false
    )
    {
        AnsiConsole.MarkupLine("");
        var tree = new Tree($"[blue]{Markup.Escape(projectName)}/[/]");

        foreach (var (path, action) in ops)
        {
            var relativePath = Path.GetRelativePath(rootDir, path).Replace('\\', '/');
            var label =
                action == FileAction.Modify
                    ? $"[yellow]{Markup.Escape(relativePath)}[/] [dim]({(isDryRun ? "modify" : "modified")})[/]"
                : isDryRun ? $"[green]{Markup.Escape(relativePath)}[/] [dim](create)[/]"
                : $"[green]{Markup.Escape(relativePath)}[/]";
            tree.AddNode(label);
        }

        AnsiConsole.Write(tree);
    }
}
