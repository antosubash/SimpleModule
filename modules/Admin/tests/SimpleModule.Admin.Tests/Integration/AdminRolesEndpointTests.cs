using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace Admin.Tests.Integration;

[Collection(TestCollections.Integration)]
public class AdminRolesEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public AdminRolesEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

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

    private async Task<string> SeedTestRoleAsync(string? name = null)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var roleName = name ?? $"TestRole-{Guid.NewGuid().ToString()[..8]}";
        var role = new ApplicationRole
        {
            Name = roleName,
            Description = "Test role for integration tests",
        };
        await roleManager.CreateAsync(role);

        return role.Id;
    }

    [Fact]
    public async Task GetRoles_AsAdmin_Returns200()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRoles_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var response = await client.GetAsync("/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRolesCreate_AsAdmin_Returns200()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/admin/roles/create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRole_ValidData_Redirects()
    {
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["name"] = $"NewRole-{Guid.NewGuid().ToString()[..8]}",
                ["description"] = "A new test role",
            }
        );

        var response = await client.PostAsync("/admin/roles", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task CreateRole_EmptyName_RedirectsToCreate()
    {
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string> { ["name"] = "" }
        );

        var response = await client.PostAsync("/admin/roles", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/admin/roles/create");
    }

    [Fact]
    public async Task GetRolesEdit_ExistingRole_Returns200()
    {
        var roleId = await SeedTestRoleAsync();
        var client = CreateAdminClient();

        var response = await client.GetAsync($"/admin/roles/{roleId}/edit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRolesEdit_NonExistentRole_Returns404()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/admin/roles/nonexistent/edit");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRole_ValidData_Redirects()
    {
        var roleId = await SeedTestRoleAsync();
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["name"] = $"UpdatedRole-{Guid.NewGuid().ToString()[..8]}",
                ["description"] = "Updated description",
            }
        );

        var response = await client.PostAsync($"/admin/roles/{roleId}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task DeleteRole_NoUsers_Redirects()
    {
        var roleId = await SeedTestRoleAsync();
        var client = CreateAdminClient();

        var response = await client.DeleteAsync($"/admin/roles/{roleId}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task DeleteRole_WithUsers_ReturnsBadRequest()
    {
        var roleName = $"RoleWithUser-{Guid.NewGuid().ToString()[..8]}";
        var roleId = await SeedTestRoleAsync(roleName);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = $"roleuser-{Guid.NewGuid().ToString()[..8]}@example.com",
            Email = $"roleuser-{Guid.NewGuid().ToString()[..8]}@example.com",
            DisplayName = "Role Test User",
        };
        await userManager.CreateAsync(user, "TestPass123!");
        await userManager.AddToRoleAsync(user, roleName);

        var client = CreateAdminClient();

        var response = await client.DeleteAsync($"/admin/roles/{roleId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRole_NonExistent_Returns404()
    {
        var client = CreateAdminClient();

        var response = await client.DeleteAsync("/admin/roles/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
