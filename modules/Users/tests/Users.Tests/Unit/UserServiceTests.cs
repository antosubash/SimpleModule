using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleModule.Core.Ids;
using SimpleModule.Users;
using SimpleModule.Users.Contracts;
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

        var user = await _sut.GetUserByIdAsync(UserId.From("1"));

        user.Should().NotBeNull();
        user!.Id.Should().Be(UserId.From("1"));
        user.DisplayName.Should().Be("Test User");
        user.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistingUser_ReturnsNull()
    {
        _userManager.FindByIdAsync("999").Returns((ApplicationUser?)null);

        var user = await _sut.GetUserByIdAsync(UserId.From("999"));

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

        var user = await _sut.GetCurrentUserAsync(UserId.From("1"));

        user.Should().NotBeNull();
        user!.Id.Should().Be(UserId.From("1"));
    }

    [Fact]
    public async Task CreateUserAsync_WithValidData_ReturnsUserDto()
    {
        _userManager
            .CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var u = callInfo.Arg<ApplicationUser>();
                u.Id = "new-id";
                return IdentityResult.Success;
            });

        var request = new CreateUserRequest
        {
            Email = "new@test.com",
            DisplayName = "New User",
            Password = "TestPass1234",
        };

        var user = await _sut.CreateUserAsync(request);

        user.Should().NotBeNull();
        user.Email.Should().Be("new@test.com");
        user.DisplayName.Should().Be("New User");
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ThrowsValidationException()
    {
        _userManager
            .CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(
                IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "DuplicateEmail",
                        Description = "Email already taken",
                    }
                )
            );

        var request = new CreateUserRequest
        {
            Email = "dup@test.com",
            DisplayName = "Dup User",
            Password = "TestPass1234",
        };

        var act = () => _sut.CreateUserAsync(request);

        await act.Should().ThrowAsync<SimpleModule.Core.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task UpdateUserAsync_WithValidData_UpdatesUser()
    {
        var appUser = new ApplicationUser
        {
            Id = "1",
            Email = "old@test.com",
            DisplayName = "Old Name",
        };
        _userManager.FindByIdAsync("1").Returns(appUser);
        _userManager.UpdateAsync(Arg.Any<ApplicationUser>()).Returns(IdentityResult.Success);

        var request = new UpdateUserRequest { Email = "new@test.com", DisplayName = "New Name" };

        var user = await _sut.UpdateUserAsync(UserId.From("1"), request);

        user.Should().NotBeNull();
        user.Email.Should().Be("new@test.com");
        user.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentUser_ThrowsNotFoundException()
    {
        _userManager.FindByIdAsync("999").Returns((ApplicationUser?)null);

        var request = new UpdateUserRequest { Email = "test@test.com", DisplayName = "Test" };

        var act = () => _sut.UpdateUserAsync(UserId.From("999"), request);

        await act.Should().ThrowAsync<SimpleModule.Core.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task DeleteUserAsync_WithExistingUser_DeletesUser()
    {
        var appUser = new ApplicationUser
        {
            Id = "1",
            Email = "test@test.com",
            DisplayName = "Test",
        };
        _userManager.FindByIdAsync("1").Returns(appUser);
        _userManager.DeleteAsync(Arg.Any<ApplicationUser>()).Returns(IdentityResult.Success);

        await _sut.DeleteUserAsync(UserId.From("1"));

        await _userManager.Received(1).DeleteAsync(appUser);
    }

    [Fact]
    public async Task DeleteUserAsync_WithNonExistentUser_ThrowsNotFoundException()
    {
        _userManager.FindByIdAsync("999").Returns((ApplicationUser?)null);

        var act = () => _sut.DeleteUserAsync(UserId.From("999"));

        await act.Should().ThrowAsync<SimpleModule.Core.Exceptions.NotFoundException>();
    }
}
