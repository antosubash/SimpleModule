using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class PagesRegistryFixerTests : IDisposable
{
    private readonly string _tempDir;

    public PagesRegistryFixerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void AddEntry_AppendsToExistingPagesIndex()
    {
        var indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(indexPath, """
            export const pages: Record<string, any> = {
                "Products/Browse": () => import("../Views/Browse"),
            };
            """);
        PagesRegistryFixer.AddEntry(indexPath, "Products/Create", "../Views/Create");
        var content = File.ReadAllText(indexPath);
        content.Should().Contain("\"Products/Create\": () => import(\"../Views/Create\")");
        content.Should().Contain("\"Products/Browse\"");
    }

    [Fact]
    public void AddEntry_CreatesFileFromScratchWhenMissing()
    {
        var indexPath = Path.Combine(_tempDir, "index.ts");
        PagesRegistryFixer.AddEntry(indexPath, "Products/Create", "../Views/Create");
        var content = File.ReadAllText(indexPath);
        content.Should().Contain("export const pages");
        content.Should().Contain("\"Products/Create\": () => import(\"../Views/Create\")");
    }
}
