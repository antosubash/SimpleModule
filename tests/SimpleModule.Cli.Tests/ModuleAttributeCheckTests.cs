using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ModuleAttributeCheckTests : IDisposable
{
    private readonly string _tempDir;

    public ModuleAttributeCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolutionWithModule(
        string moduleName,
        string? moduleClassContent = null
    )
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        Directory.CreateDirectory(moduleDir);
        if (moduleClassContent is not null)
            File.WriteAllText(
                Path.Combine(moduleDir, $"{moduleName}Module.cs"),
                moduleClassContent
            );
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenModuleAttributePresentWithRoutePrefix()
    {
        var solution = CreateSolutionWithModule(
            "Products",
            """
            [Module("Products", RoutePrefix = "products")]
            public class ProductsModule : IModule { }
            """
        );
        var results = new ModuleAttributeCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products [Module] attribute" && r.Status == CheckStatus.Pass
            );
    }

    [Fact]
    public void Run_Fail_WhenModuleClassHasNoModuleAttribute()
    {
        var solution = CreateSolutionWithModule(
            "Products",
            """
            public class ProductsModule : IModule { }
            """
        );
        var results = new ModuleAttributeCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products [Module] attribute" && r.Status == CheckStatus.Fail
            );
    }

    [Fact]
    public void Run_Warning_WhenModuleClassFileMissing()
    {
        var solution = CreateSolutionWithModule("Products", moduleClassContent: null);
        var results = new ModuleAttributeCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products [Module] attribute" && r.Status == CheckStatus.Warning
            );
    }

    [Fact]
    public void Run_Fail_WhenModuleAttributeMissingRoutePrefix()
    {
        var solution = CreateSolutionWithModule(
            "Products",
            """
            [Module("Products")]
            public class ProductsModule : IModule { }
            """
        );
        var results = new ModuleAttributeCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products [Module] attribute" && r.Status == CheckStatus.Fail
            );
    }
}
