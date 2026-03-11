using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleModule.Users;
using SimpleModule.Users.Entities;

namespace Users.Tests.Unit;

public sealed class UserServiceTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );
        _sut = new UserService(_userManager, NullLogger<UserService>.Instance);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ReturnsUserDto()
    {
        var appUser = new ApplicationUser
        {
            Id = "1",
            Email = "test@test.com",
            DisplayName = "Test User",
            EmailConfirmed = true,
        };
        _userManager.FindByIdAsync("1").Returns(appUser);

        var user = await _sut.GetUserByIdAsync("1");

        user.Should().NotBeNull();
        user!.Id.Should().Be("1");
        user.DisplayName.Should().Be("Test User");
        user.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistingUser_ReturnsNull()
    {
        _userManager.FindByIdAsync("999").Returns((ApplicationUser?)null);

        var user = await _sut.GetUserByIdAsync("999");

        user.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_DelegatesToGetUserByIdAsync()
    {
        var appUser = new ApplicationUser
        {
            Id = "1",
            Email = "test@test.com",
            DisplayName = "Test User",
        };
        _userManager.FindByIdAsync("1").Returns(appUser);

        var user = await _sut.GetCurrentUserAsync("1");

        user.Should().NotBeNull();
        user!.Id.Should().Be("1");
    }
}
