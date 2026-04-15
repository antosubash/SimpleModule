using SimpleModule.Cli.Commands.New;

namespace SimpleModule.Cli.Tests;

[Trait("Category", "Integration")]
public sealed partial class NewProjectScaffoldTests : IDisposable
{
    private const string TestVersion = "0.0.15";
    private readonly string _tempDir;

    public NewProjectScaffoldTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-scaffold-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup
        }
    }

    private (string ProjectName, string RootDir) ScaffoldStandalone(string projectName = "TestApp")
    {
        var rootDir = Path.Combine(_tempDir, projectName);
        NewProjectCommand.ScaffoldProject(
            projectName,
            rootDir,
            solution: null,
            frameworkVersion: TestVersion
        );
        return (projectName, rootDir);
    }
}
