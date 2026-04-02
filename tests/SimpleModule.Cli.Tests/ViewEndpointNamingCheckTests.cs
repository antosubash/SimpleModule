using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ViewEndpointNamingCheckTests : IDisposable
{
    private readonly string _tempDir;

    public ViewEndpointNamingCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolutionWithEndpoints(
        string moduleName,
        params string[] endpointFileNames
    )
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var endpointsDir = Path.Combine(
            _tempDir,
            "src",
            "modules",
            moduleName,
            "src",
            moduleName,
            "Endpoints",
            moduleName
        );
        Directory.CreateDirectory(endpointsDir);
        foreach (var name in endpointFileNames)
            File.WriteAllText(Path.Combine(endpointsDir, name), "// stub");
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenAllEndpointFilesFollowConvention()
    {
        var solution = CreateSolutionWithEndpoints(
            "Products",
            "GetAllEndpoint.cs",
            "CreateEndpoint.cs"
        );
        var results = new ViewEndpointNamingCheck().Run(solution).ToList();
        results.Should().OnlyContain(r => r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Warn_WhenEndpointFileDoesNotEndWithEndpoint()
    {
        var solution = CreateSolutionWithEndpoints("Products", "GetProducts.cs");
        var results = new ViewEndpointNamingCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r => r.Name.Contains("GetProducts") && r.Status == CheckStatus.Warning);
    }

    [Fact]
    public void Run_Pass_WhenNoEndpointsDirectory()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", "Products", "src", "Products");
        Directory.CreateDirectory(moduleDir);
        var solution = SolutionContext.Discover(_tempDir)!;
        var results = new ViewEndpointNamingCheck().Run(solution).ToList();
        results.Should().BeEmpty();
    }
}
