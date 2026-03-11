using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class CollapseBlankLinesTests
{
    [Fact]
    public void ConsecutiveBlanks_CollapsedToSingle()
    {
        var lines = new List<string> { "a", "", "", "", "b" };
        var result = TemplateExtractor.CollapseBlankLines(lines);

        result.Should().Equal("a", "", "b");
    }

    [Fact]
    public void SingleBlanks_PreservedAsIs()
    {
        var lines = new List<string> { "a", "", "b", "", "c" };
        var result = TemplateExtractor.CollapseBlankLines(lines);

        result.Should().Equal("a", "", "b", "", "c");
    }

    [Fact]
    public void NoBlanks_NoChange()
    {
        var lines = new List<string> { "a", "b", "c" };
        var result = TemplateExtractor.CollapseBlankLines(lines);

        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void EmptyList_ReturnsEmpty()
    {
        TemplateExtractor.CollapseBlankLines([]).Should().BeEmpty();
    }

    [Fact]
    public void AllBlanks_CollapsedToOne()
    {
        var lines = new List<string> { "", "", "" };
        var result = TemplateExtractor.CollapseBlankLines(lines);

        result.Should().Equal("");
    }

    [Fact]
    public void WhitespaceOnly_TreatedAsBlank()
    {
        var lines = new List<string> { "a", "   ", "  ", "b" };
        var result = TemplateExtractor.CollapseBlankLines(lines);

        result.Should().HaveCount(3);
        result[0].Should().Be("a");
        result[2].Should().Be("b");
    }
}
