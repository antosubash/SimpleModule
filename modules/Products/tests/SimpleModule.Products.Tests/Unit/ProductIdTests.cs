using FluentAssertions;
using SimpleModule.Products.Contracts;

namespace Products.Tests.Unit;

public sealed class ProductIdTests
{
    [Fact]
    public void From_WithValidInt_CreatesProductId()
    {
        var id = ProductId.From(42);

        id.Value.Should().Be(42);
    }

    [Fact]
    public void From_WithZero_CreatesProductId()
    {
        var id = ProductId.From(0);

        id.Value.Should().Be(0);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var id1 = ProductId.From(7);
        var id2 = ProductId.From(7);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var id1 = ProductId.From(1);
        var id2 = ProductId.From(2);

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ToString_ReturnsValueString()
    {
        var id = ProductId.From(5);

        id.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Contain("5");
    }
}
