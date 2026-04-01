using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.New;

public sealed class NewAgentCommand : Command<NewAgentSettings>
{
    public override int Execute(CommandContext context, NewAgentSettings settings)
    {
        var agentName = settings.ResolveName();

        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]"
            );
            return 1;
        }

        if (solution.ExistingModules.Count == 0)
        {
            AnsiConsole.MarkupLine(
                "[red]No modules found. Create a module first with 'sm new module'.[/]"
            );
            return 1;
        }

        var moduleName = settings.ResolveModule(solution.ExistingModules);

        if (!solution.ExistingModules.Contains(moduleName, StringComparer.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine(
                $"[red]Module '{Markup.Escape(moduleName)}' not found. Available: {Markup.Escape(string.Join(", ", solution.ExistingModules))}[/]"
            );
            return 1;
        }

        var moduleDir = solution.GetModuleProjectPath(moduleName);
        var agentsDir = Path.Combine(moduleDir, "Agents");
        var knowledgeDir = Path.Combine(agentsDir, "Knowledge");

        var ops = new List<(string Path, string Content)>
        {
            (
                Path.Combine(agentsDir, $"{agentName}Agent.cs"),
                GenerateAgentDefinition(moduleName, agentName)
            ),
            (
                Path.Combine(agentsDir, $"{agentName}ToolProvider.cs"),
                GenerateToolProvider(moduleName, agentName)
            ),
            (
                Path.Combine(agentsDir, $"{agentName}KnowledgeSource.cs"),
                GenerateKnowledgeSource(moduleName, agentName)
            ),
        };

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry run — no files will be written.[/]");
            foreach (var (path, _) in ops)
            {
                var rel = Path.GetRelativePath(solution.RootPath, path);
                AnsiConsole.MarkupLine($"  [green]CREATE[/] {Markup.Escape(rel)}");
            }

            AnsiConsole.MarkupLine(
                $"  [green]CREATE[/] {Markup.Escape(Path.GetRelativePath(solution.RootPath, knowledgeDir))}/"
            );
            return 0;
        }

        Directory.CreateDirectory(agentsDir);
        Directory.CreateDirectory(knowledgeDir);

        foreach (var (path, content) in ops)
        {
            if (File.Exists(path))
            {
                AnsiConsole.MarkupLine(
                    $"  [yellow]SKIP[/] {Markup.Escape(Path.GetRelativePath(solution.RootPath, path))} (already exists)"
                );
                continue;
            }

            File.WriteAllText(path, content);
            AnsiConsole.MarkupLine(
                $"  [green]CREATE[/] {Markup.Escape(Path.GetRelativePath(solution.RootPath, path))}"
            );
        }

        AnsiConsole.MarkupLine(
            $"\n[green]Agent '{Markup.Escape(agentName)}' created in module '{Markup.Escape(moduleName)}'.[/]"
        );
        return 0;
    }

    private static string GenerateAgentDefinition(string moduleName, string agentName) =>
        $$"""
            using SimpleModule.Core.Agents;

            namespace SimpleModule.{{moduleName}}.Agents;

            public class {{agentName}}Agent : IAgentDefinition
            {
                public string Name => "{{ToKebabCase(agentName)}}";

                public string Description => "{{agentName}} agent for {{moduleName}}";

                public string Instructions =>
                    \"\"\"
                    You are a helpful assistant for the {{moduleName}} module.
                    Use the available tools to answer user questions.
                    \"\"\";
            }
            """;

    private static string GenerateToolProvider(string moduleName, string agentName) =>
        $$"""
            using SimpleModule.Core.Agents;

            namespace SimpleModule.{{moduleName}}.Agents;

            public class {{agentName}}ToolProvider : IAgentToolProvider
            {
                [AgentTool(Description = "Example tool — replace with real implementation")]
                public Task<string> ExampleTool(string query) =>
                    Task.FromResult($"Result for: {query}");
            }
            """;

    private static string GenerateKnowledgeSource(string moduleName, string agentName) =>
        $$"""
            using SimpleModule.Core.Rag;

            namespace SimpleModule.{{moduleName}}.Agents;

            public class {{agentName}}KnowledgeSource : IKnowledgeSource
            {
                public string CollectionName => "{{ToKebabCase(agentName)}}-knowledge";

                public Task<IReadOnlyList<KnowledgeDocument>> GetDocumentsAsync(
                    CancellationToken cancellationToken
                ) =>
                    Task.FromResult<IReadOnlyList<KnowledgeDocument>>(
                        [
                            new(
                                "{{moduleName}} Overview",
                                "Add your {{moduleName}} module knowledge documents here."
                            ),
                        ]
                    );
            }
            """;

    private static string ToKebabCase(string pascalCase)
    {
        var chars = new List<char>();
        for (var i = 0; i < pascalCase.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascalCase[i]))
            {
                chars.Add('-');
            }

            chars.Add(char.ToLowerInvariant(pascalCase[i]));
        }

        return new string(chars.ToArray());
    }
}
