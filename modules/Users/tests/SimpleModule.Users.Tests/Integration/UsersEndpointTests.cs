using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Tests.Shared.Fixtures;
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
    public async Task GetUserById_Authenticated_WithInvalidId_ReturnsNotFound()
    {
        var client = _factory.CreateAuthenticatedClient();

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
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/users/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
