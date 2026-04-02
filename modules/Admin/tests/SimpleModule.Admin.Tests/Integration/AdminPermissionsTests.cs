using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace Admin.Tests.Integration;

public class AdminPermissionsTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public AdminPermissionsTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.NameIdentifier, "admin-test-id"),
        };
        var claimsValue = string.Join(";", claims.Select(c => $"{c.Type}={c.Value}"));
        client.DefaultRequestHeaders.Add("X-Test-Claims", claimsValue);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        return client;
    }

    [Fact]
    public async Task SetUserPermissions_ValidData_Redirects()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = $"perm-test-{Guid.NewGuid():N}@test.com",
            Email = $"perm-test-{Guid.NewGuid():N}@test.com",
            DisplayName = "Permission Test User",
        };
        await userManager.CreateAsync(user, "TestPass123!");

        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string> { { "permissions", "Admin.ManageUsers" } }
        );

        var response = await client.PostAsync($"/admin/users/{user.Id}/permissions", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task SetRolePermissions_ValidData_Redirects()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = new ApplicationRole
        {
            Name = $"PermTestRole-{Guid.NewGuid().ToString()[..8]}",
            Description = "Test role for permissions",
        };
        await roleManager.CreateAsync(role);

        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string> { { "permissions", "Admin.ManageUsers" } }
        );

        var response = await client.PostAsync($"/admin/roles/{role.Id}/permissions", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task CreateRoleWithPermissions_AssignsPermissions()
    {
        var client = CreateAdminClient();
        var roleName = $"WithPerms-{Guid.NewGuid().ToString()[..8]}";

        using var content = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("name", roleName),
                new KeyValuePair<string, string>("description", "Test"),
                new KeyValuePair<string, string>("permissions", "Admin.ManageUsers"),
                new KeyValuePair<string, string>("permissions", "Admin.ManageRoles"),
            }
        );

        var response = await client.PostAsync("/admin/roles", content);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = await roleManager.FindByNameAsync(roleName);
        role.Should().NotBeNull();

        var permContracts = scope.ServiceProvider.GetRequiredService<IPermissionContracts>();
        var perms = await permContracts.GetPermissionsForRoleAsync(RoleId.From(role!.Id));
        perms.Should().HaveCount(2);
        perms.Should().Contain("Admin.ManageUsers");
        perms.Should().Contain("Admin.ManageRoles");
    }

    [Fact]
    public async Task DeleteRole_ClearsPermissions()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = new ApplicationRole
        {
            Name = $"DelPermRole-{Guid.NewGuid().ToString()[..8]}",
            Description = "To be deleted",
        };
        await roleManager.CreateAsync(role);

        var permContracts = scope.ServiceProvider.GetRequiredService<IPermissionContracts>();
        await permContracts.SetPermissionsForRoleAsync(
            RoleId.From(role.Id),
            ["Admin.ManageUsers", "Admin.ManageRoles"]
        );

        var client = CreateAdminClient();
        var response = await client.DeleteAsync($"/admin/roles/{role.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope2 = _factory.Services.CreateScope();
        var permContracts2 = scope2.ServiceProvider.GetRequiredService<IPermissionContracts>();
        var perms = await permContracts2.GetPermissionsForRoleAsync(RoleId.From(role.Id));
        perms.Should().BeEmpty();
    }

    [Fact]
    public async Task SetUserPermissions_CanBeVerifiedViaContracts()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = $"verify-perm-{Guid.NewGuid():N}@test.com",
            Email = $"verify-perm-{Guid.NewGuid():N}@test.com",
            DisplayName = "Verify Perm User",
        };
        await userManager.CreateAsync(user, "TestPass123!");

        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("permissions", "Admin.ManageUsers"),
                new KeyValuePair<string, string>("permissions", "Admin.ManageRoles"),
            }
        );

        await client.PostAsync($"/admin/users/{user.Id}/permissions", content);

        using var scope2 = _factory.Services.CreateScope();
        var permContracts = scope2.ServiceProvider.GetRequiredService<IPermissionContracts>();
        var perms = await permContracts.GetPermissionsForUserAsync(UserId.From(user.Id));
        perms.Should().HaveCount(2);
        perms.Should().Contain("Admin.ManageUsers");
        perms.Should().Contain("Admin.ManageRoles");
    }
}
