using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ReadAndTransformTests : IDisposable
{
    private readonly string _tempDir;

    public ReadAndTransformTests()
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

    private string CreateFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ReplacesModuleNamesInFile()
    {
        var path = CreateFile(
            "OrderService.cs",
            """
            namespace SimpleModule.Orders;

            public class OrderService : IOrderContracts
            {
                // route: /orders
            }
            """
        );

        var result = TemplateExtractor.ReadAndTransform(
            path,
            "Orders",
            "Order",
            "Invoices",
            "Invoice"
        );

        result.Should().Contain("namespace SimpleModule.Invoices;");
        result.Should().Contain("InvoiceService");
        result.Should().Contain("IInvoiceContracts");
        result.Should().Contain("/invoices");
    }

    [Fact]
    public void StripsLinesContainingPatterns()
    {
        var path = CreateFile(
            "Test.cs",
            """
            using SimpleModule.Core;
            using SimpleModule.Products.Contracts;
            using SimpleModule.Users.Contracts;

            namespace SimpleModule.Orders;
            """
        );

        var result = TemplateExtractor.ReadAndTransform(
            path,
            "Orders",
            "Order",
            "Invoices",
            "Invoice",
            stripLinesContaining: ["SimpleModule.Products", "SimpleModule.Users"]
        );

        result.Should().Contain("SimpleModule.Core");
        result.Should().NotContain("Products");
        result.Should().NotContain("Users");
    }

    [Fact]
    public void CollapsesBlankLinesAfterStripping()
    {
        var path = CreateFile("Test.cs", "a\nremove1\n\nremove2\n\nb");

        var result = TemplateExtractor.ReadAndTransform(
            path,
            "X",
            "X",
            "Y",
            "Y",
            stripLinesContaining: ["remove1", "remove2"]
        );

        // After stripping, blank lines should be collapsed
        result
            .Should()
            .NotContain(Environment.NewLine + Environment.NewLine + Environment.NewLine);
    }

    [Fact]
    public void NoStripPatterns_JustRenames()
    {
        var path = CreateFile("Simple.cs", "class Order { }");

        var result = TemplateExtractor.ReadAndTransform(
            path,
            "Orders",
            "Order",
            "Invoices",
            "Invoice"
        );

        result.Should().Be("class Invoice { }");
    }
}
