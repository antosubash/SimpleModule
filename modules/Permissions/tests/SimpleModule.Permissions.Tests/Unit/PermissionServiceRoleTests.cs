using FluentAssertions;
using Permissions.Tests.Helpers;
using SimpleModule.Permissions;
using SimpleModule.Permissions.Contracts;

namespace Permissions.Tests.Unit;

public sealed class PermissionServiceRoleTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetPermissionsForRole_NoPermissions_ReturnsEmpty()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);

        var result = await sut.GetPermissionsForRoleAsync(RoleId.From("role-1"));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetPermissionsForRole_ThenGet_ReturnsSetPermissions()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var roleId = RoleId.From("role-1");

        await sut.SetPermissionsForRoleAsync(roleId, ["read", "write"]);
        var result = await sut.GetPermissionsForRoleAsync(roleId);

        result.Should().HaveCount(2);
        result.Should().Contain("read");
        result.Should().Contain("write");
    }

    [Fact]
    public async Task SetPermissionsForRole_ReplacesExisting()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var roleId = RoleId.From("role-1");

        await sut.SetPermissionsForRoleAsync(roleId, ["read", "write"]);
        await sut.SetPermissionsForRoleAsync(roleId, ["delete"]);
        var result = await sut.GetPermissionsForRoleAsync(roleId);

        result.Should().HaveCount(1);
        result.Should().Contain("delete");
        result.Should().NotContain("read");
        result.Should().NotContain("write");
    }

    [Fact]
    public async Task SetPermissionsForRole_EmptyList_ClearsAll()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var roleId = RoleId.From("role-1");

        await sut.SetPermissionsForRoleAsync(roleId, ["read", "write"]);
        await sut.SetPermissionsForRoleAsync(roleId, []);
        var result = await sut.GetPermissionsForRoleAsync(roleId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsForRole_IsolatedBetweenRoles()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var role1 = RoleId.From("role-1");
        var role2 = RoleId.From("role-2");

        await sut.SetPermissionsForRoleAsync(role1, ["read"]);
        await sut.SetPermissionsForRoleAsync(role2, ["write"]);

        var result1 = await sut.GetPermissionsForRoleAsync(role1);
        var result2 = await sut.GetPermissionsForRoleAsync(role2);

        result1.Should().HaveCount(1);
        result1.Should().Contain("read");
        result2.Should().HaveCount(1);
        result2.Should().Contain("write");
    }

    public void Dispose() => _factory.Dispose();
}
