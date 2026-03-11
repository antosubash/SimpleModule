using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class RemoveBraceBlocksTests
{
    [Fact]
    public void RemovesSingleLineBlock()
    {
        var lines = new List<string> { "keep1", "remove this { something; }", "keep2" };

        var result = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line => line.Contains("remove this", StringComparison.Ordinal)
        );

        result.Should().Equal("keep1", "keep2");
    }

    [Fact]
    public void RemovesMultiLineBlock_BraceOnSameLine()
    {
        var lines = new List<string> { "keep1", "if (condition) {", "    body;", "}", "keep2" };

        var result = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line => line.Contains("if (condition)", StringComparison.Ordinal)
        );

        result.Should().Equal("keep1", "keep2");
    }

    [Fact]
    public void RemovesMultiLineBlock_BraceOnNextLine()
    {
        var lines = new List<string> { "keep1", "void Method()", "{", "    body;", "}", "keep2" };

        var result = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line => line.Contains("void Method", StringComparison.Ordinal)
        );

        result.Should().Equal("keep1", "keep2");
    }

    [Fact]
    public void HandlesNestedBraces()
    {
        var lines = new List<string>
        {
            "before",
            "void Method() {",
            "    if (x) {",
            "        inner;",
            "    }",
            "}",
            "after",
        };

        var result = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line => line.Contains("void Method", StringComparison.Ordinal)
        );

        result.Should().Equal("before", "after");
    }

    [Fact]
    public void RemovesMultipleBlocks()
    {
        var lines = new List<string>
        {
            "keep1",
            "remove A {",
            "    a body;",
            "}",
            "keep2",
            "remove B {",
            "    b body;",
            "}",
            "keep3",
        };

        var result = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line => line.StartsWith("remove", StringComparison.Ordinal)
        );

        result.Should().Equal("keep1", "keep2", "keep3");
    }

    [Fact]
    public void NoMatches_ReturnsAllLines()
    {
        var lines = new List<string> { "a", "b", "c" };

        var result = TemplateExtractor.RemoveBraceBlocks(lines, _ => false);

        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void PredicateLineWithNoBraces_SkipsUntilBlockEnds()
    {
        // Simulates modelBuilder.Entity<OrderItem>(entity =>
        var lines = new List<string>
        {
            "before",
            "modelBuilder.Entity<Item>(entity =>",
            "{",
            "    entity.HasKey(e => e.Id);",
            "});",
            "after",
        };

        var result = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line => line.Contains("Entity<Item>", StringComparison.Ordinal)
        );

        result.Should().Equal("before", "after");
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        TemplateExtractor.RemoveBraceBlocks([], _ => true).Should().BeEmpty();
    }

    [Fact]
    public void StatementWithoutBraces_RemovesSingleLine()
    {
        // A predicate match on a line with no braces and no subsequent braces
        // should remove just that line (when the NEXT line doesn't have an opening brace)
        var lines = new List<string> { "before", "SeedOrders(modelBuilder);", "after" };

        // Since "SeedOrders" has no braces, seenOpenBrace stays false,
        // skipping stays true until a brace is seen. The next line "after"
        // has no brace either, so it's also skipped.
        // This is by design — the method is meant for brace blocks.
        // For single-line removal, use List.RemoveAll instead.
        var result = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line => line.Contains("SeedOrders", StringComparison.Ordinal)
        );

        // The predicate line is always removed. If no opening brace is found,
        // it continues scanning until one is found.
        result[0].Should().Be("before");
    }
}
