using System.Net;
using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Users.Tests.Integration;

public class AdminRolesEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public AdminRolesEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAdminClient() =>
        _factory.CreateAuthenticatedClient(new Claim(ClaimTypes.Role, "Admin"));

    [Fact]
    public async Task ListRoles_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/admin/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListRoles_NonAdmin_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/admin/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListRoles_Admin_ReturnsOk()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/admin/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListRoles_WithInertiaHeader_ReturnsJson()
    {
        var client = CreateAdminClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "1");
        var response = await client.GetAsync("/admin/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task CreateRolePage_Admin_ReturnsOk()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/admin/roles/create");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRole_WithEmptyName_Redirects()
    {
        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string> { ["name"] = "", ["description"] = "Test" }
        );
        // Disable auto-redirect to check the redirect status
        var handler = _factory.Server.CreateHandler();
        using var noRedirectClient = new HttpClient(handler)
        {
            BaseAddress = _factory.Server.BaseAddress,
        };
        noRedirectClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        var claims = $"{ClaimTypes.Role}=Admin;{ClaimTypes.NameIdentifier}=test-user-id";
        noRedirectClient.DefaultRequestHeaders.Add("X-Test-Claims", claims);

        var response = await noRedirectClient.PostAsync("/admin/roles", content);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Be("/admin/roles/create");
    }

    [Fact]
    public async Task EditRole_NonexistentId_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/admin/roles/nonexistent-id/edit");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRole_NonexistentId_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["name"] = "Updated",
                ["description"] = "Updated description",
            }
        );
        var response = await client.PostAsync("/admin/roles/nonexistent-id", content);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRole_NonexistentId_ReturnsNotFound()
    {
        var client = CreateAdminClient();
        var response = await client.DeleteAsync("/admin/roles/nonexistent-id");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
