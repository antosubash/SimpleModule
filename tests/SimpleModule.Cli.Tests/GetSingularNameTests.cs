using FluentAssertions;
using SimpleModule.Cli.Templates;

namespace SimpleModule.Cli.Tests;

public sealed class GetSingularNameTests
{
    [Theory]
    [InlineData("Orders", "Order")]
    [InlineData("Products", "Product")]
    [InlineData("Users", "User")]
    [InlineData("Invoices", "Invoice")]
    [InlineData("Items", "Item")]
    public void StandardPlurals_RemovesTrailingS(string plural, string expected)
    {
        ModuleTemplates.GetSingularName(plural).Should().Be(expected);
    }

    [Theory]
    [InlineData("Categories", "Category")]
    [InlineData("Companies", "Company")]
    [InlineData("Policies", "Policy")]
    [InlineData("Entries", "Entry")]
    public void IesPlurals_ReplacesWithY(string plural, string expected)
    {
        ModuleTemplates.GetSingularName(plural).Should().Be(expected);
    }

    [Theory]
    [InlineData("Address", "Address")]
    [InlineData("Glass", "Glass")]
    [InlineData("Access", "Access")]
    public void DoubleSS_LeavesUnchanged(string input, string expected)
    {
        ModuleTemplates.GetSingularName(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Data", "Data")]
    [InlineData("Auth", "Auth")]
    public void NoPlural_ReturnsUnchanged(string input, string expected)
    {
        ModuleTemplates.GetSingularName(input).Should().Be(expected);
    }
}
