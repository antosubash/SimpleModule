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
        ApiCsprojPath = DiscoverApiCsproj(rootPath);
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

    public string GetModuleViewsPath(string moduleName) =>
        Path.Combine(ModulesPath, moduleName, "src", moduleName, "Views");

    public string GetModulePagesIndexPath(string moduleName) =>
        Path.Combine(ModulesPath, moduleName, "src", moduleName, "Pages", "index.ts");

    private static string DiscoverApiCsproj(string rootPath)
    {
        var srcPath = Path.Combine(rootPath, "src");

        if (Directory.Exists(srcPath))
        {
            // Look for directories ending with .Host
            foreach (var dir in Directory.GetDirectories(srcPath, "*.Host"))
            {
                var dirName = Path.GetFileName(dir);
                var csproj = Path.Combine(dir, $"{dirName}.csproj");
                if (File.Exists(csproj))
                {
                    return csproj;
                }
            }

            // Fall back to directories ending with .Api
            foreach (var dir in Directory.GetDirectories(srcPath, "*.Api"))
            {
                var dirName = Path.GetFileName(dir);
                var csproj = Path.Combine(dir, $"{dirName}.csproj");
                if (File.Exists(csproj))
                {
                    return csproj;
                }
            }
        }

        // Fall back to template path
        var templatePath = Path.Combine(
            rootPath,
            "template",
            "SimpleModule.Host",
            "SimpleModule.Host.csproj"
        );
        if (File.Exists(templatePath))
        {
            return templatePath;
        }

        // Last resort: original hardcoded path
        return Path.Combine(rootPath, "src", "SimpleModule.Host", "SimpleModule.Host.csproj");
    }
}
