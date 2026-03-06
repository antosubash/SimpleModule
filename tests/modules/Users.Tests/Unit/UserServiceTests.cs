using FluentAssertions;
using SimpleModule.Users;
using SimpleModule.Users.Contracts;

namespace Users.Tests.Unit;

public class UserServiceTests
{
    private readonly UserService _sut = new();

    [Fact]
    public async Task GetAllUsersAsync_ReturnsNonEmptyCollection()
    {
        var users = await _sut.GetAllUsersAsync();

        users.Should().NotBeEmpty();
        users
            .Should()
            .AllSatisfy(u =>
            {
                u.Id.Should().BeGreaterThan(0);
                u.Name.Should().NotBeNullOrWhiteSpace();
            });
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUserWithMatchingId()
    {
        var user = await _sut.GetUserByIdAsync(42);

        user.Should().NotBeNull();
        user!.Id.Should().Be(42);
    }
}
