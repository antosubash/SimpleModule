using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ReplaceModuleNamesTests
{
    [Fact]
    public void ReplacesPlural_ThenSingular_ThenLowercase()
    {
        var content = "namespace SimpleModule.Orders; class Order { route = \"/orders\" }";
        var result = TemplateExtractor.ReplaceModuleNames(
            content, "Orders", "Order", "Invoices", "Invoice");

        result.Should().Be("namespace SimpleModule.Invoices; class Invoice { route = \"/invoices\" }");
    }

    [Fact]
    public void PluralReplacedFirst_PreventsDoubleReplacement()
    {
        // "Orders" contains "Order" — plural must be replaced first
        var content = "Orders Order orders";
        var result = TemplateExtractor.ReplaceModuleNames(
            content, "Orders", "Order", "Products", "Product");

        result.Should().Be("Products Product products");
    }

    [Fact]
    public void HandlesLowercaseRoutes()
    {
        var content = "MapGroup(\"/orders\")";
        var result = TemplateExtractor.ReplaceModuleNames(
            content, "Orders", "Order", "Invoices", "Invoice");

        result.Should().Be("MapGroup(\"/invoices\")");
    }

    [Fact]
    public void DoesNotReplace_WhenCaseDoesNotMatch()
    {
        var content = "ORDERS orders Orders";
        var result = TemplateExtractor.ReplaceModuleNames(
            content, "Orders", "Order", "Invoices", "Invoice");

        // "ORDERS" is not matched (Ordinal comparison), "orders" is matched as lowercase
        result.Should().Be("ORDERS invoices Invoices");
    }

    [Fact]
    public void EmptyContent_ReturnsEmpty()
    {
        TemplateExtractor.ReplaceModuleNames("", "Orders", "Order", "Invoices", "Invoice")
            .Should().BeEmpty();
    }

    [Fact]
    public void NoMatches_ReturnsOriginal()
    {
        var content = "something unrelated";
        TemplateExtractor.ReplaceModuleNames(content, "Orders", "Order", "Invoices", "Invoice")
            .Should().Be(content);
    }
}
