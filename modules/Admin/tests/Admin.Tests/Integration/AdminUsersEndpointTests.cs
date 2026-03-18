using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Entities;

namespace Admin.Tests.Integration;

public class AdminUsersEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public AdminUsersEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

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

    private async Task<string> SeedTestUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"testuser-{userId[..8]}@example.com",
            Email = $"testuser-{userId[..8]}@example.com",
            DisplayName = "Test User",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(user, "TestPass123!");

        return userId;
    }

    [Fact]
    public async Task GetUsers_AsAdmin_Returns200()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsers_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsersCreate_AsAdmin_Returns200()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/admin/users/create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Skip = "UsersEditEndpoint queries AdminDbContext which requires matching DatabaseOptions in test setup")]
    public async Task GetUsersEdit_ExistingUser_Returns200()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        var response = await client.GetAsync($"/admin/users/{userId}/edit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsersEdit_NonExistentUser_Returns404()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/admin/users/nonexistent/edit");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_ValidData_Redirects()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Updated Name",
            ["email"] = $"updated-{userId[..8]}@example.com",
        });

        var response = await client.PostAsync($"/admin/users/{userId}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task LockUser_ValidUser_Redirects()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        var response = await client.PostAsync($"/admin/users/{userId}/lock", null);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task UnlockUser_ValidUser_Redirects()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        var response = await client.PostAsync($"/admin/users/{userId}/unlock", null);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task DeactivateUser_ValidUser_Redirects()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        var response = await client.PostAsync($"/admin/users/{userId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ReactivateUser_ValidUser_Redirects()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        await client.PostAsync($"/admin/users/{userId}/deactivate", null);

        var response = await client.PostAsync($"/admin/users/{userId}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ResetPassword_ValidData_Redirects()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["newPassword"] = "NewTestPass456!",
        });

        var response = await client.PostAsync($"/admin/users/{userId}/reset-password", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact(Skip = "UsersActivityEndpoint queries AdminDbContext which requires matching DatabaseOptions in test setup")]
    public async Task GetActivity_ValidUser_Returns200()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        var response = await client.GetAsync($"/admin/users/{userId}/activity");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
