using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Users.Tests.Integration;

public class AdminUsersEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public AdminUsersEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAdminClient() =>
        _factory.CreateAuthenticatedClient(new Claim(ClaimTypes.Role, "Admin"));

    [Fact]
    public async Task ListUsers_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListUsers_NonAdmin_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListUsers_Admin_ReturnsOk()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListUsers_WithInertiaHeader_ReturnsJson()
    {
        var client = CreateAdminClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "1");
        var response = await client.GetAsync("/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ListUsers_WithSearch_ReturnsOk()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/admin/users?search=admin");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EditUser_NonexistentId_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/admin/users/nonexistent-id/edit");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_NonexistentId_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "New Name",
            ["email"] = "new@example.com",
        });
        var response = await client.PostAsync("/admin/users/nonexistent-id", content);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetRoles_NonexistentId_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["roles"] = "Admin",
        });
        var response = await client.PostAsync("/admin/users/nonexistent-id/roles", content);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LockUser_NonexistentId_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        var response = await client.PostAsync("/admin/users/nonexistent-id/lock", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlockUser_NonexistentId_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        var response = await client.PostAsync("/admin/users/nonexistent-id/unlock", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
