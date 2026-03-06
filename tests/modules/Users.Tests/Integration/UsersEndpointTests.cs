using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace Users.Tests.Integration;

public class UsersEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllUsers_Returns200WithUserList()
    {
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<User>>();
        users.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUserById_Returns200WithUser()
    {
        var response = await _client.GetAsync("/api/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<User>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(1);
    }
}
