using SimpleModule.Cli.Infrastructure;
using SimpleModule.Cli.Templates;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewModuleCommand : Command<NewModuleSettings>
{
    public override int Execute(CommandContext context, NewModuleSettings settings)
    {
        var moduleName = settings.ResolveName();
        var singularName = ModuleTemplates.GetSingularName(moduleName);

        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]"
            );
            return 1;
        }

        if (solution.ExistingModules.Contains(moduleName, StringComparer.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine(
                $"[red]Module '{Markup.Escape(moduleName)}' already exists. Available modules: {Markup.Escape(string.Join(", ", solution.ExistingModules))}[/]"
            );
            return 1;
        }

        var templates = new ModuleTemplates(solution);
        var ops = new List<(string Path, FileAction Action)>();

        var contractsDir = solution.GetModuleContractsPath(moduleName);
        var moduleDir = solution.GetModuleProjectPath(moduleName);
        var eventsDir = Path.Combine(contractsDir, "Events");
        var endpointsDir = Path.Combine(moduleDir, "Endpoints", moduleName);
        var testDir = solution.GetTestProjectPath(moduleName);

        void Plan(string path) => ops.Add((path, FileAction.Create));
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
        Plan(Path.Combine(testDir, $"{moduleName}.Tests.csproj"));
        Plan(Path.Combine(testDir, "GlobalUsings.cs"));
        Plan(Path.Combine(testDir, "Unit", $"{singularName}ServiceTests.cs"));
        Plan(Path.Combine(testDir, "Integration", $"{moduleName}EndpointTests.cs"));
        ops.Add((solution.SlnxPath, FileAction.Modify));
        ops.Add((solution.ApiCsprojPath, FileAction.Modify));

        if (settings.DryRun)
        {
            RenderDryRunTree(moduleName, ops);
            return 0;
        }

        AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .Start(
                $"Creating module '{moduleName}'...",
                ctx =>
                {
                    Directory.CreateDirectory(eventsDir);
                    Directory.CreateDirectory(endpointsDir);
                    Directory.CreateDirectory(Path.Combine(testDir, "Unit"));
                    Directory.CreateDirectory(Path.Combine(testDir, "Integration"));

                    File.WriteAllText(
                        Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"),
                        templates.ContractsCsproj(moduleName)
                    );
                    File.WriteAllText(
                        Path.Combine(contractsDir, $"I{singularName}Contracts.cs"),
                        templates.ContractsInterface(moduleName, singularName)
                    );
                    File.WriteAllText(
                        Path.Combine(contractsDir, $"{singularName}.cs"),
                        templates.DtoClass(moduleName, singularName)
                    );
                    File.WriteAllText(
                        Path.Combine(eventsDir, $"{singularName}CreatedEvent.cs"),
                        templates.EventClass(moduleName, singularName)
                    );

                    File.WriteAllText(
                        Path.Combine(moduleDir, $"{moduleName}.csproj"),
                        templates.ModuleCsproj(moduleName)
                    );
                    File.WriteAllText(
                        Path.Combine(moduleDir, $"{moduleName}Module.cs"),
                        templates.ModuleClass(moduleName, singularName)
                    );
                    File.WriteAllText(
                        Path.Combine(moduleDir, $"{moduleName}Constants.cs"),
                        templates.ConstantsClass(moduleName, singularName)
                    );
                    File.WriteAllText(
                        Path.Combine(moduleDir, $"{moduleName}DbContext.cs"),
                        templates.DbContextClass(moduleName, singularName)
                    );
                    File.WriteAllText(
                        Path.Combine(moduleDir, $"{singularName}Service.cs"),
                        templates.ServiceClass(moduleName, singularName)
                    );
                    File.WriteAllText(
                        Path.Combine(endpointsDir, "GetAllEndpoint.cs"),
                        templates.GetAllEndpoint(moduleName, singularName)
                    );

                    File.WriteAllText(
                        Path.Combine(testDir, $"{moduleName}.Tests.csproj"),
                        templates.TestCsproj(moduleName)
                    );
                    File.WriteAllText(
                        Path.Combine(testDir, "GlobalUsings.cs"),
                        templates.GlobalUsings()
                    );
                    File.WriteAllText(
                        Path.Combine(testDir, "Unit", $"{singularName}ServiceTests.cs"),
                        templates.UnitTestSkeleton(moduleName, singularName)
                    );
                    File.WriteAllText(
                        Path.Combine(testDir, "Integration", $"{moduleName}EndpointTests.cs"),
                        templates.IntegrationTestSkeleton(moduleName, singularName)
                    );

                    ctx.Status("Updating solution files...");
                    SlnxManipulator.AddModuleEntries(solution.SlnxPath, moduleName);
                    ProjectManipulator.AddProjectReference(
                        solution.ApiCsprojPath,
                        $@"..\modules\{moduleName}\src\{moduleName}\{moduleName}.csproj"
                    );
                }
            );

        RenderCreatedTree(moduleName, ops);

        AnsiConsole.MarkupLine($"\n[green]Module '{Markup.Escape(moduleName)}' created![/]");
        AnsiConsole.MarkupLine("[dim]Next steps:[/]");
        AnsiConsole.MarkupLine(
            $"[dim]  sm new feature <FeatureName> --module {Markup.Escape(moduleName)}[/]"
        );
        AnsiConsole.MarkupLine("[dim]  dotnet build[/]");
        return 0;
    }

    private static void RenderDryRunTree(
        string moduleName,
        List<(string Path, FileAction Action)> ops
    )
    {
        AnsiConsole.MarkupLine("[dim]Dry run — no files written[/]\n");
        var tree = new Tree($"[blue]{Markup.Escape(moduleName)}[/]");
        foreach (var (path, action) in ops)
        {
            var label =
                action == FileAction.Modify
                    ? $"[yellow]{Markup.Escape(Path.GetFileName(path))}[/] [dim](modify)[/]"
                    : $"[green]{Markup.Escape(Path.GetFileName(path))}[/] [dim](create)[/]";
            tree.AddNode(label);
        }
        AnsiConsole.Write(tree);
    }

    private static void RenderCreatedTree(
        string moduleName,
        List<(string Path, FileAction Action)> ops
    )
    {
        AnsiConsole.MarkupLine("");
        var tree = new Tree($"[blue]{Markup.Escape(moduleName)}[/]");
        foreach (var (path, action) in ops)
        {
            var label =
                action == FileAction.Modify
                    ? $"[yellow]{Markup.Escape(Path.GetFileName(path))}[/] [dim](modified)[/]"
                    : $"[green]{Markup.Escape(Path.GetFileName(path))}[/]";
            tree.AddNode(label);
        }
        AnsiConsole.Write(tree);
    }
}
