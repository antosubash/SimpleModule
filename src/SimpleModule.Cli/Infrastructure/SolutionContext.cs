namespace SimpleModule.Cli.Infrastructure;

public sealed class SolutionContext
{
    public string RootPath { get; }
    public string SlnxPath { get; }
    public string ApiCsprojPath { get; }
    public string ModulesPath { get; }

    public IReadOnlyList<string> ExistingModules { get; }

    private SolutionContext(string rootPath, string slnxPath)
    {
        RootPath = rootPath;
        SlnxPath = slnxPath;
        ApiCsprojPath = Path.Combine(
            rootPath,
            "src",
            "SimpleModule.Api",
            "SimpleModule.Api.csproj"
        );
        ModulesPath = Path.Combine(rootPath, "src", "modules");

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

    public static SolutionContext? Discover(string? startDir = null)
    {
        var dir = startDir ?? Directory.GetCurrentDirectory();

        while (dir is not null)
        {
            var slnxFiles = Directory.GetFiles(dir, "*.slnx");
            if (slnxFiles.Length > 0)
            {
                return new SolutionContext(dir, slnxFiles[0]);
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }

    public string GetModulePath(string moduleName) => Path.Combine(ModulesPath, moduleName);

    public string GetModuleContractsPath(string moduleName) =>
        Path.Combine(ModulesPath, moduleName, "src", $"{moduleName}.Contracts");

    public string GetModuleProjectPath(string moduleName) =>
        Path.Combine(ModulesPath, moduleName, "src", moduleName);

    public string GetTestProjectPath(string moduleName) =>
        Path.Combine(ModulesPath, moduleName, "tests", $"{moduleName}.Tests");
}
