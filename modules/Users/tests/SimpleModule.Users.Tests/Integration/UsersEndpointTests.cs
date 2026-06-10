using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users;
using SimpleModule.Users.Contracts;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public class UsersEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;
    private readonly HttpClient _unauthenticatedClient;

    public UsersEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
        _unauthenticatedClient = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllUsers_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _unauthenticatedClient.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUsers_Authenticated_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserById_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _unauthenticatedClient.GetAsync("/api/users/some-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_WithoutViewPermission_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/users/some-id");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserById_Authenticated_WithInvalidId_ReturnsNotFound()
    {
        var client = _factory.CreateAuthenticatedClient([UsersPermissions.View]);

        var response = await client.GetAsync("/api/users/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentUser_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _unauthenticatedClient.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_Authenticated_WithNoMatchingUser_ReturnsNotFound()
    {
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, "nonexistent-user")
        );

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_Unauthenticated_ReturnsUnauthorized()
    {
        var request = new CreateUserRequest
        {
            Email = "new@test.com",
            DisplayName = "New User",
            Password = "TestPass1234",
        };

        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_Authenticated_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new CreateUserRequest
        {
            Email = "newuser@test.com",
            DisplayName = "New User",
            Password = "TestPass1234",
        };

        var response = await client.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateUser_Unauthenticated_ReturnsUnauthorized()
    {
        var request = new UpdateUserRequest { Email = "updated@test.com", DisplayName = "Updated" };

        var response = await _unauthenticatedClient.PutAsJsonAsync("/api/users/some-id", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteUser_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _unauthenticatedClient.DeleteAsync("/api/users/some-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteUser_Authenticated_WithNonExistentId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([UsersPermissions.Delete]);

        var response = await client.DeleteAsync("/api/users/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Instance-level authorization (UserPolicy) ---------------------------------

    private async Task<UserDto> CreateUserAsync(string email)
    {
        var admin = _factory.CreateAuthenticatedClient(new Claim(ClaimTypes.Role, "Admin"));
        var response = await admin.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                Email = email,
                DisplayName = "Policy Target",
                Password = "TestPass1234",
            }
        );
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<UserDto>();
        dto.Should().NotBeNull();
        return dto!;
    }

    private HttpClient ClientFor(string userId, params string[] permissions) =>
        _factory.CreateAuthenticatedClient(
            permissions,
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

    private HttpClient AdminClient() =>
        _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.NameIdentifier, "admin-tester")
        );

    [Fact]
    public async Task GetUserById_NonAdminViewingAnotherUser_Returns404()
    {
        var target = await CreateUserAsync("view-target@test.com");
        var client = ClientFor("a-different-user", UsersPermissions.View);

        var response = await client.GetAsync($"/api/users/{target.Id.Value}");

        // DenyAsNotFound — a non-owner cannot tell the account exists.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_NonAdminViewingSelf_ReturnsOk()
    {
        var target = await CreateUserAsync("view-self@test.com");
        var client = ClientFor(target.Id.Value, UsersPermissions.View);

        var response = await client.GetAsync($"/api/users/{target.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserById_Admin_CanViewAnotherUser()
    {
        var target = await CreateUserAsync("view-admin@test.com");

        var response = await AdminClient().GetAsync($"/api/users/{target.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_NonAdminUpdatingAnotherUser_ReturnsForbidden()
    {
        var target = await CreateUserAsync("update-other@test.com");
        var client = ClientFor("a-different-user", UsersPermissions.Update);
        var request = new UpdateUserRequest { Email = target.Email, DisplayName = "Hacked" };

        var response = await client.PutAsJsonAsync($"/api/users/{target.Id.Value}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUser_NonAdminUpdatingSelf_ReturnsOk()
    {
        var target = await CreateUserAsync("update-self@test.com");
        var client = ClientFor(target.Id.Value, UsersPermissions.Update);
        var request = new UpdateUserRequest
        {
            Email = target.Email,
            DisplayName = "Renamed Self",
        };

        var response = await client.PutAsJsonAsync($"/api/users/{target.Id.Value}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteUser_NonAdminWithPermission_ReturnsForbidden()
    {
        var target = await CreateUserAsync("delete-nonadmin@test.com");
        // Even the owner, with the Delete permission, may not delete — admin-only.
        var client = ClientFor(target.Id.Value, UsersPermissions.Delete);

        var response = await client.DeleteAsync($"/api/users/{target.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_Admin_Returns204()
    {
        var target = await CreateUserAsync("delete-admin@test.com");

        var response = await AdminClient().DeleteAsync($"/api/users/{target.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
