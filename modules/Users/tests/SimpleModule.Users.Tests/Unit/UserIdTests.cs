using FluentAssertions;
using SimpleModule.Users.Contracts;
using Vogen;

namespace Users.Tests.Unit;

public sealed class UserIdTests
{
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
}
