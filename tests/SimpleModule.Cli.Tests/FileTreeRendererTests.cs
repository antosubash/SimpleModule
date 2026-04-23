using System.Reflection;
using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class FileTreeRendererTests
{
    private static string InvokeFormatNode(
        string path,
        FileAction action,
        string? rootDir,
        bool isDryRun
    )
    {
        var method = typeof(FileTreeRenderer).GetMethod(
            "FormatNode",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;
        return (string)method.Invoke(null, [path, action, rootDir, isDryRun])!;
    }

    [Fact]
    public void FormatNode_Create_DryRun_RendersGreenCreateLabel()
    {
        var result = InvokeFormatNode(
            "/x/Foo.cs",
            FileAction.Create,
            rootDir: null,
            isDryRun: true
        );

        result.Should().Contain("Foo.cs").And.Contain("(create)").And.Contain("[green]");
    }

    [Fact]
    public void FormatNode_Create_NonDryRun_RendersGreenWithoutActionTag()
    {
        var result = InvokeFormatNode(
            "/x/Foo.cs",
            FileAction.Create,
            rootDir: null,
            isDryRun: false
        );

        result.Should().Contain("Foo.cs").And.Contain("[green]");
        result.Should().NotContain("(create)").And.NotContain("(modify)");
    }

    [Fact]
    public void FormatNode_Modify_DryRun_UsesModifyWord()
    {
        var result = InvokeFormatNode(
            "/x/Existing.cs",
            FileAction.Modify,
            rootDir: null,
            isDryRun: true
        );

        result.Should().Contain("(modify)").And.Contain("[yellow]");
    }

    [Fact]
    public void FormatNode_Modify_NonDryRun_UsesModifiedWord()
    {
        var result = InvokeFormatNode(
            "/x/Existing.cs",
            FileAction.Modify,
            rootDir: null,
            isDryRun: false
        );

        result.Should().Contain("(modified)").And.Contain("[yellow]");
    }

    [Fact]
    public void FormatNode_NoRootDir_ShowsFileNameOnly()
    {
        var nestedPath = Path.Combine("some", "deeply", "nested", "Foo.cs");

        var result = InvokeFormatNode(nestedPath, FileAction.Create, rootDir: null, isDryRun: true);

        result.Should().Contain("Foo.cs");
        result.Should().NotContain("nested").And.NotContain("deeply");
    }

    [Fact]
    public void FormatNode_WithRootDir_ShowsRelativePathWithForwardSlashes()
    {
        var rootDir = Path.Combine("proj", "root");
        var filePath = Path.Combine(rootDir, "src", "modules", "Foo.cs");

        var result = InvokeFormatNode(filePath, FileAction.Create, rootDir, isDryRun: true);

        result.Should().Contain("src/modules/Foo.cs");
        result.Should().NotContain("\\");
    }
}
