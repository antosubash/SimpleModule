using FluentAssertions;
using Permissions.Tests.Helpers;
using SimpleModule.Permissions;
using SimpleModule.Users.Contracts;

namespace Permissions.Tests.Unit;

public sealed class PermissionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetPermissionsForUser_NoPermissions_ReturnsEmpty()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);

        var result = await sut.GetPermissionsForUserAsync(UserId.From("user-1"));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetPermissionsForUser_ThenGet_ReturnsSetPermissions()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        await sut.SetPermissionsForUserAsync(userId, ["read", "write"]);
        var result = await sut.GetPermissionsForUserAsync(userId);

        result.Should().HaveCount(2);
        result.Should().Contain("read");
        result.Should().Contain("write");
    }

    [Fact]
    public async Task SetPermissionsForUser_ReplacesExisting()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        await sut.SetPermissionsForUserAsync(userId, ["read", "write"]);
        await sut.SetPermissionsForUserAsync(userId, ["delete"]);
        var result = await sut.GetPermissionsForUserAsync(userId);

        result.Should().HaveCount(1);
        result.Should().Contain("delete");
        result.Should().NotContain("read");
        result.Should().NotContain("write");
    }

    [Fact]
    public async Task SetPermissionsForUser_EmptyList_ClearsAll()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        await sut.SetPermissionsForUserAsync(userId, ["read", "write"]);
        await sut.SetPermissionsForUserAsync(userId, []);
        var result = await sut.GetPermissionsForUserAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsForUser_IsolatedBetweenUsers()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var user1 = UserId.From("user-1");
        var user2 = UserId.From("user-2");

        await sut.SetPermissionsForUserAsync(user1, ["read"]);
        await sut.SetPermissionsForUserAsync(user2, ["write"]);

        var result1 = await sut.GetPermissionsForUserAsync(user1);
        var result2 = await sut.GetPermissionsForUserAsync(user2);

        result1.Should().HaveCount(1);
        result1.Should().Contain("read");
        result2.Should().HaveCount(1);
        result2.Should().Contain("write");
    }

    public void Dispose() => _factory.Dispose();
}
