using FluentAssertions;
using SimpleModule.Users.Contracts;
using Vogen;

namespace Users.Tests.Unit;

public sealed class UserIdTests
{
    [Fact]
    public void From_WithValidString_CreatesUserId()
    {
        var id = UserId.From("user-123");

        id.Value.Should().Be("user-123");
    }

    [Fact]
    public void From_WithEmptyString_ThrowsException()
    {
        var act = () => UserId.From(string.Empty);

        act.Should().Throw<ValueObjectValidationException>();
    }

    [Fact]
    public void From_WithWhitespace_ThrowsException()
    {
        var act = () => UserId.From("   ");

        act.Should().Throw<ValueObjectValidationException>();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var id1 = UserId.From("user-abc");
        var id2 = UserId.From("user-abc");

        id1.Should().Be(id2);
    }

    [Fact]
    public void ToString_ReturnsValueString()
    {
        var id = UserId.From("user-456");

        id.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Contain("user-456");
    }
}
