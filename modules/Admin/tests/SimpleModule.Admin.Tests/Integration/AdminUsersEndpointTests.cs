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
public class AdminUsersEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public AdminUsersEndpointTests(SimpleModuleWebApplicationFactory factory)
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

    private async Task<ApplicationUser> FetchUserAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.Should().NotBeNull($"user {userId} should exist for assertion");
        return user!;
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
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

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

    [Fact]
    public async Task GetUsersEdit_NonExistentUser_Returns404()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/admin/users/nonexistent/edit");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_ValidData_PersistsDisplayNameAndEmail()
    {
        var userId = await SeedTestUserAsync();
        var newEmail = $"updated-{userId[..8]}@example.com";
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["displayName"] = "Updated Name",
                ["email"] = newEmail,
            }
        );

        var response = await client.PostAsync($"/admin/users/{userId}", content);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var user = await FetchUserAsync(userId);
        user.DisplayName.Should().Be("Updated Name");
        user.Email.Should().Be(newEmail);
    }

    [Fact]
    public async Task LockUser_SetsLockoutEndInFuture()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        var response = await client.PostAsync($"/admin/users/{userId}/lock", null);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var user = await FetchUserAsync(userId);
        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task UnlockUser_ClearsLockout()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        await client.PostAsync($"/admin/users/{userId}/lock", null);
        (await FetchUserAsync(userId)).LockoutEnd.Should().NotBeNull();

        var response = await client.PostAsync($"/admin/users/{userId}/unlock", null);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var user = await FetchUserAsync(userId);
        // UnlockAccountAsync sets LockoutEnd to null or to a past timestamp.
        (user.LockoutEnd is null || user.LockoutEnd.Value <= DateTimeOffset.UtcNow)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task DeactivateUser_SetsDeactivatedAt()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        var response = await client.PostAsync($"/admin/users/{userId}/deactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var user = await FetchUserAsync(userId);
        user.DeactivatedAt.Should().NotBeNull();
        user.DeactivatedAt!.Value.Should()
            .BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ReactivateUser_ClearsDeactivatedAt()
    {
        var userId = await SeedTestUserAsync();
        var client = CreateAdminClient();

        await client.PostAsync($"/admin/users/{userId}/deactivate", null);
        (await FetchUserAsync(userId)).DeactivatedAt.Should().NotBeNull();

        var response = await client.PostAsync($"/admin/users/{userId}/reactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        (await FetchUserAsync(userId)).DeactivatedAt.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_ReplacesPasswordHash_NewPasswordAuthenticates()
    {
        var userId = await SeedTestUserAsync();
        var oldHash = (await FetchUserAsync(userId)).PasswordHash;
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string> { ["newPassword"] = "NewTestPass456!" }
        );

        var response = await client.PostAsync($"/admin/users/{userId}/reset-password", content);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotBe(oldHash, "password reset must replace the hash");
        (await userManager.CheckPasswordAsync(user, "NewTestPass456!")).Should().BeTrue();
        (await userManager.CheckPasswordAsync(user, "TestPass123!"))
            .Should()
            .BeFalse("the old password must no longer authenticate");
    }

    [Fact]
    public async Task LockUser_Self_ReturnsBadRequest()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsync("/admin/users/admin-test-id/lock", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeactivateUser_Self_ReturnsBadRequest()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsync("/admin/users/admin-test-id/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForcePhoneReverify_ValidUser_ClearsPhoneNumberConfirmedAndRedirects()
    {
        var userId = await SeedTestUserAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>
            >();
            var user = await userManager.FindByIdAsync(userId);
            await userManager.SetPhoneNumberAsync(user!, "+15550009999");
            user!.PhoneNumberConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        var client = CreateAdminClient();
        var response = await client.PostAsync($"/admin/users/{userId}/force-phone-reverify", null);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope2 = _factory.Services.CreateScope();
        var userManager2 = scope2.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
        var after = await userManager2.FindByIdAsync(userId);
        after!.PhoneNumberConfirmed.Should().BeFalse();
        after.PhoneNumber.Should().Be("+15550009999", "phone number itself should be preserved");
    }
}
