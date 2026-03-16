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
                "[red]Could not find .slnx file. Run this command from within a SimpleModule project.[/]"
            );
            return 1;
        }

        if (solution.ExistingModules.Contains(moduleName, StringComparer.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine($"[red]Module '{moduleName}' already exists.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"[blue]Creating module '{moduleName}' (singular: '{singularName}')...[/]"
        );

        var templates = new ModuleTemplates(solution);

        // Create directories
        var contractsDir = solution.GetModuleContractsPath(moduleName);
        var moduleDir = solution.GetModuleProjectPath(moduleName);
        var eventsDir = Path.Combine(contractsDir, "Events");
        var endpointsDir = Path.Combine(moduleDir, "Endpoints", moduleName);
        var testDir = solution.GetTestProjectPath(moduleName);
        var unitTestDir = Path.Combine(testDir, "Unit");
        var integrationTestDir = Path.Combine(testDir, "Integration");

        Directory.CreateDirectory(eventsDir);
        Directory.CreateDirectory(endpointsDir);
        Directory.CreateDirectory(unitTestDir);
        Directory.CreateDirectory(integrationTestDir);

        // Contracts project files
        WriteFile(
            Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"),
            templates.ContractsCsproj(moduleName)
        );
        WriteFile(
            Path.Combine(contractsDir, $"I{singularName}Contracts.cs"),
            templates.ContractsInterface(moduleName, singularName)
        );
        WriteFile(
            Path.Combine(contractsDir, $"{singularName}.cs"),
            templates.DtoClass(moduleName, singularName)
        );
        WriteFile(
            Path.Combine(eventsDir, $"{singularName}CreatedEvent.cs"),
            templates.EventClass(moduleName, singularName)
        );

        // Module project files
        WriteFile(
            Path.Combine(moduleDir, $"{moduleName}.csproj"),
            templates.ModuleCsproj(moduleName)
        );
        WriteFile(
            Path.Combine(moduleDir, $"{moduleName}Module.cs"),
            templates.ModuleClass(moduleName, singularName)
        );
        WriteFile(
            Path.Combine(moduleDir, $"{moduleName}Constants.cs"),
            templates.ConstantsClass(moduleName, singularName)
        );
        WriteFile(
            Path.Combine(moduleDir, $"{moduleName}DbContext.cs"),
            templates.DbContextClass(moduleName, singularName)
        );
        WriteFile(
            Path.Combine(moduleDir, $"{singularName}Service.cs"),
            templates.ServiceClass(moduleName, singularName)
        );
        WriteFile(
            Path.Combine(endpointsDir, "GetAllEndpoint.cs"),
            templates.GetAllEndpoint(moduleName, singularName)
        );

        // Test project files
        WriteFile(
            Path.Combine(testDir, $"{moduleName}.Tests.csproj"),
            templates.TestCsproj(moduleName)
        );
        WriteFile(Path.Combine(testDir, "GlobalUsings.cs"), templates.GlobalUsings());
        WriteFile(
            Path.Combine(unitTestDir, $"{singularName}ServiceTests.cs"),
            templates.UnitTestSkeleton(moduleName, singularName)
        );
        WriteFile(
            Path.Combine(integrationTestDir, $"{moduleName}EndpointTests.cs"),
            templates.IntegrationTestSkeleton(moduleName, singularName)
        );

        // Modify solution files
        AnsiConsole.MarkupLine("[blue]Updating solution files...[/]");

        SlnxManipulator.AddModuleEntries(solution.SlnxPath, moduleName);
        AnsiConsole.MarkupLine("[green]  + .slnx entries added[/]");

        ProjectManipulator.AddProjectReference(
            solution.ApiCsprojPath,
            $@"..\modules\{moduleName}\src\{moduleName}\{moduleName}.csproj"
        );
        AnsiConsole.MarkupLine("[green]  + API project reference added[/]");

        AnsiConsole.MarkupLine($"[green]Module '{moduleName}' created successfully![/]");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[dim]Files created:[/]");
        AnsiConsole.MarkupLine($"[dim]  src/modules/{moduleName}/src/ (10 files)[/]");
        AnsiConsole.MarkupLine($"[dim]  src/modules/{moduleName}/tests/ (3 files)[/]");

        return 0;
    }

    private static void WriteFile(string path, string content)
    {
        File.WriteAllText(path, content);
        var relativePath = Path.GetFileName(path);
        AnsiConsole.MarkupLine($"[green]  + {relativePath}[/]");
    }
}
