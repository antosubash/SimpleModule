using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Users;
using SimpleModule.Users.Contracts;

namespace Users.Tests.Unit;

public sealed class UserServiceTests : IDisposable
{
    private readonly UsersDbContext _db;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new UsersDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new UserService(_db);
    }

    public void Dispose() => _db.Dispose();

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
        var user = await _sut.GetUserByIdAsync(1);

        user.Should().NotBeNull();
        user!.Id.Should().Be(1);
    }
}
