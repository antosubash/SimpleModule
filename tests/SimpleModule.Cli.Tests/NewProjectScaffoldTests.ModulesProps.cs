using FluentAssertions;

namespace SimpleModule.Cli.Tests;

public sealed partial class NewProjectScaffoldTests
{
    [Fact]
    public void Scaffold_WritesModulesDirectoryBuildProps_WithModuleKind()
    {
        var (_, rootDir) = ScaffoldStandalone();

        var propsPath = Path.Combine(rootDir, "src", "modules", "Directory.Build.props");
        File.Exists(propsPath).Should().BeTrue();

        var content = File.ReadAllText(propsPath);
        content.Should().Contain("<SimpleModuleProjectKind");
        content.Should().Contain("CompilerVisibleProperty Include=\"SimpleModuleProjectKind\"");
        content.Should().Contain("PackageReference Include=\"SimpleModule.Generator\"");
        content.Should().Contain("GetPathOfFileAbove");
    }
}
