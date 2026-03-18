using FluentAssertions;
using SimpleModule.Orders.Contracts;

namespace Orders.Tests.Unit;

public sealed class OrderIdTests
{
    [Fact]
    public void From_WithValidInt_CreatesOrderId()
    {
        var id = OrderId.From(42);

        id.Value.Should().Be(42);
    }

    [Fact]
    public void From_WithZero_CreatesOrderId()
    {
        var id = OrderId.From(0);

        id.Value.Should().Be(0);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var id1 = OrderId.From(7);
        var id2 = OrderId.From(7);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var id1 = OrderId.From(1);
        var id2 = OrderId.From(2);

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ToString_ReturnsValueString()
    {
        var id = OrderId.From(5);

        id.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Contain("5");
    }
}
