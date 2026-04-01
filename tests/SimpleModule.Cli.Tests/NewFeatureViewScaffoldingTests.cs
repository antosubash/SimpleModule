using FluentAssertions;
using SimpleModule.Cli.Infrastructure;
using SimpleModule.Cli.Templates;

namespace SimpleModule.Cli.Tests;

public sealed class NewFeatureViewScaffoldingTests : IDisposable
{
    private readonly string _tempDir;

    public NewFeatureViewScaffoldingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolution()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void ViewComponent_ContainsFeatureNameAsComponentName()
    {
        var result = FeatureTemplates.ViewComponent("Products", "Create");
        result.Should().Contain("export default function Create");
    }

    [Fact]
    public void ViewComponent_ContainsPropsType()
    {
        var result = FeatureTemplates.ViewComponent("Products", "Browse");
        result.Should().Contain("type Props =");
    }

    [Fact]
    public void ViewComponent_ContainsHeadingWithFeatureName()
    {
        var result = FeatureTemplates.ViewComponent("Products", "Create");
        result.Should().Contain("<h1>Create</h1>");
    }

    [Fact]
    public void PagesRegistryFixer_AddsEntryForNewFeature()
    {
        var pagesDir = Path.Combine(_tempDir, "Pages");
        Directory.CreateDirectory(pagesDir);
        var indexPath = Path.Combine(pagesDir, "index.ts");
        File.WriteAllText(
            indexPath,
            """
            export const pages: Record<string, any> = {
                "Products/Browse": () => import("../Views/Browse"),
            };
            """
        );

        PagesRegistryFixer.AddEntry(indexPath, "Products/Create", "../Views/Create");

        var content = File.ReadAllText(indexPath);
        content.Should().Contain("\"Products/Create\": () => import(\"../Views/Create\")");
        content.Should().Contain("\"Products/Browse\"");
    }
}
