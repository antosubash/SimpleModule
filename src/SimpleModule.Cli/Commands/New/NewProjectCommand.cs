using SimpleModule.Cli.Infrastructure;
using SimpleModule.Cli.Templates;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewProjectCommand : Command<NewProjectSettings>
{
    public override int Execute(CommandContext context, NewProjectSettings settings)
    {
        var projectName = settings.ResolveName();
        var outputDir = settings.ResolveOutputDir();
        var rootDir = Path.Combine(outputDir, projectName);

        if (Directory.Exists(rootDir) && Directory.GetFileSystemEntries(rootDir).Length > 0)
        {
            AnsiConsole.MarkupLine($"[red]Directory '{rootDir}' already exists and is not empty.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[blue]Creating project '{projectName}' in {rootDir}...[/]");

        // Try to discover an existing solution for reference templates
        var solution = SolutionContext.Discover();
        var templates = new ProjectTemplates(solution);

        // Create directory structure
        var srcDir = Path.Combine(rootDir, "src");
        var apiDir = Path.Combine(srcDir, $"{projectName}.Api");
        var coreDir = Path.Combine(srcDir, $"{projectName}.Core");
        var databaseDir = Path.Combine(srcDir, $"{projectName}.Database");
        var generatorDir = Path.Combine(srcDir, $"{projectName}.Generator");
        var modulesDir = Path.Combine(srcDir, "modules");
        var testsDir = Path.Combine(rootDir, "tests");
        var testsSharedDir = Path.Combine(testsDir, $"{projectName}.Tests.Shared");
        var testsModulesDir = Path.Combine(testsDir, "modules");

        Directory.CreateDirectory(apiDir);
        Directory.CreateDirectory(coreDir);
        Directory.CreateDirectory(databaseDir);
        Directory.CreateDirectory(generatorDir);
        Directory.CreateDirectory(modulesDir);
        Directory.CreateDirectory(testsSharedDir);
        Directory.CreateDirectory(testsModulesDir);

        // Root files
        WriteFile(Path.Combine(rootDir, $"{projectName}.slnx"), templates.Slnx(projectName));
        WriteFile(Path.Combine(rootDir, "Directory.Build.props"), templates.DirectoryBuildProps());
        WriteFile(Path.Combine(rootDir, "Directory.Packages.props"), templates.DirectoryPackagesProps());
        WriteFile(Path.Combine(rootDir, "global.json"), templates.GlobalJson());

        // Project files
        WriteFile(Path.Combine(apiDir, $"{projectName}.Api.csproj"), templates.ApiCsproj(projectName));
        WriteFile(Path.Combine(apiDir, "Program.cs"), ProjectTemplates.ApiProgram());
        WriteFile(Path.Combine(coreDir, $"{projectName}.Core.csproj"), templates.CoreCsproj(projectName));
        WriteFile(Path.Combine(databaseDir, $"{projectName}.Database.csproj"), templates.DatabaseCsproj(projectName));
        WriteFile(Path.Combine(generatorDir, $"{projectName}.Generator.csproj"), templates.GeneratorCsproj());
        WriteFile(Path.Combine(testsSharedDir, $"{projectName}.Tests.Shared.csproj"), templates.TestsSharedCsproj(projectName));

        AnsiConsole.MarkupLine($"[green]Project '{projectName}' created successfully![/]");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[dim]Next steps:[/]");
        AnsiConsole.MarkupLine($"[dim]  cd {projectName}[/]");
        AnsiConsole.MarkupLine("[dim]  sm new module <ModuleName>[/]");
        AnsiConsole.MarkupLine("[dim]  dotnet build[/]");

        return 0;
    }

    private static void WriteFile(string path, string content)
    {
        File.WriteAllText(path, content);
        AnsiConsole.MarkupLine($"[green]  + {Path.GetFileName(path)}[/]");
    }
}
