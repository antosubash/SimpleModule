using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class SolutionContextTests : IDisposable
{
    private readonly string _tempDir;

    public SolutionContextTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Discover_ReturnsNull_WhenNoSlnxFound()
    {
        SolutionContext.Discover(_tempDir).Should().BeNull();
    }

    [Fact]
    public void Discover_FindsSlnxInCurrentDir()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        Directory.CreateDirectory(Path.Combine(_tempDir, "src", "Test.Api"));

        var ctx = SolutionContext.Discover(_tempDir);

        ctx.Should().NotBeNull();
        ctx!.RootPath.Should().Be(_tempDir);
        ctx.SlnxPath.Should().EndWith(".slnx");
    }

    [Fact]
    public void Discover_WalksUpToFindSlnx()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var subDir = Path.Combine(_tempDir, "src", "modules", "Orders");
        Directory.CreateDirectory(subDir);

        var ctx = SolutionContext.Discover(subDir);

        ctx.Should().NotBeNull();
        ctx!.RootPath.Should().Be(_tempDir);
    }

    [Fact]
    public void ExistingModules_ListsModuleDirectories()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var modulesDir = Path.Combine(_tempDir, "src", "modules");
        Directory.CreateDirectory(Path.Combine(modulesDir, "Orders"));
        Directory.CreateDirectory(Path.Combine(modulesDir, "Products"));
        Directory.CreateDirectory(Path.Combine(modulesDir, "Users"));

        var ctx = SolutionContext.Discover(_tempDir);

        ctx!.ExistingModules.Should().Equal("Orders", "Products", "Users");
    }

    [Fact]
    public void ExistingModules_SortsCaseInsensitive()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var modulesDir = Path.Combine(_tempDir, "src", "modules");
        Directory.CreateDirectory(Path.Combine(modulesDir, "Zebra"));
        Directory.CreateDirectory(Path.Combine(modulesDir, "Alpha"));

        var ctx = SolutionContext.Discover(_tempDir);

        ctx!.ExistingModules.Should().Equal("Alpha", "Zebra");
    }

    [Fact]
    public void ExistingModules_EmptyWhenNoModulesDir()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");

        var ctx = SolutionContext.Discover(_tempDir);

        ctx!.ExistingModules.Should().BeEmpty();
    }

    [Fact]
    public void GetModulePath_ReturnsCorrectPath()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");

        var ctx = SolutionContext.Discover(_tempDir)!;

        ctx.GetModulePath("Invoices").Should().Be(
            Path.Combine(_tempDir, "src", "modules", "Invoices"));
    }

    [Fact]
    public void GetModuleContractsPath_ReturnsCorrectPath()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");

        var ctx = SolutionContext.Discover(_tempDir)!;

        ctx.GetModuleContractsPath("Invoices").Should().Be(
            Path.Combine(_tempDir, "src", "modules", "Invoices", "Invoices.Contracts"));
    }

    [Fact]
    public void GetModuleProjectPath_ReturnsCorrectPath()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");

        var ctx = SolutionContext.Discover(_tempDir)!;

        ctx.GetModuleProjectPath("Invoices").Should().Be(
            Path.Combine(_tempDir, "src", "modules", "Invoices", "Invoices"));
    }

    [Fact]
    public void GetTestProjectPath_ReturnsCorrectPath()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");

        var ctx = SolutionContext.Discover(_tempDir)!;

        ctx.GetTestProjectPath("Invoices").Should().Be(
            Path.Combine(_tempDir, "tests", "modules", "Invoices.Tests"));
    }

    [Fact]
    public void ApiCsprojPath_FollowsConvention()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");

        var ctx = SolutionContext.Discover(_tempDir)!;

        ctx.ApiCsprojPath.Should().Contain("SimpleModule.Api");
        ctx.ApiCsprojPath.Should().EndWith(".csproj");
    }
}
