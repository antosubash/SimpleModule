using FluentAssertions;
using Permissions.Tests.Helpers;
using SimpleModule.Permissions;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace Permissions.Tests.Unit;

public sealed class PermissionServiceCombinedTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetAllPermissionsForUser_CombinesUserAndRolePermissions()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");
        var roleId = RoleId.From("role-1");

        await sut.SetPermissionsForUserAsync(userId, ["user-perm"]);
        await sut.SetPermissionsForRoleAsync(roleId, ["role-perm"]);

        var result = await sut.GetAllPermissionsForUserAsync(userId, [roleId]);

        result.Should().HaveCount(2);
        result.Should().Contain("user-perm");
        result.Should().Contain("role-perm");
    }

    [Fact]
    public async Task GetAllPermissionsForUser_DeduplicatesOverlapping()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");
        var roleId = RoleId.From("role-1");

        await sut.SetPermissionsForUserAsync(userId, ["shared", "user-only"]);
        await sut.SetPermissionsForRoleAsync(roleId, ["shared", "role-only"]);

        var result = await sut.GetAllPermissionsForUserAsync(userId, [roleId]);

        result.Should().HaveCount(3);
        result.Should().Contain("shared");
        result.Should().Contain("user-only");
        result.Should().Contain("role-only");
    }

    [Fact]
    public async Task GetAllPermissionsForUser_MultipleRoles_CombinesAll()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");
        var role1 = RoleId.From("role-1");
        var role2 = RoleId.From("role-2");

        await sut.SetPermissionsForUserAsync(userId, ["user-perm"]);
        await sut.SetPermissionsForRoleAsync(role1, ["role1-perm"]);
        await sut.SetPermissionsForRoleAsync(role2, ["role2-perm"]);

        var result = await sut.GetAllPermissionsForUserAsync(userId, [role1, role2]);

        result.Should().HaveCount(3);
        result.Should().Contain("user-perm");
        result.Should().Contain("role1-perm");
        result.Should().Contain("role2-perm");
    }

    [Fact]
    public async Task GetAllPermissionsForUser_NoRoles_ReturnsOnlyUserPermissions()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        await sut.SetPermissionsForUserAsync(userId, ["user-perm"]);

        var result = await sut.GetAllPermissionsForUserAsync(userId, []);

        result.Should().HaveCount(1);
        result.Should().Contain("user-perm");
    }

    [Fact]
    public async Task GetAllPermissionsForUser_NoPermissionsAnywhere_ReturnsEmpty()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        var result = await sut.GetAllPermissionsForUserAsync(userId, []);

        result.Should().BeEmpty();
    }

    public void Dispose() => _factory.Dispose();
}
