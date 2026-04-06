using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public class PasskeyApiEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;
    private readonly HttpClient _unauthenticated;

    public PasskeyApiEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
        _unauthenticated = factory.CreateClient();
    }

    private async Task<string> SeedTestUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string userId = "passkey-test-user-id";
        var existing = await userManager.FindByIdAsync(userId);
        if (existing is not null)
            return userId;

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "passkeytest@example.com",
            Email = "passkeytest@example.com",
            DisplayName = "Passkey Test User",
        };
        await userManager.CreateAsync(user, "TestPass1234!");
        return userId;
    }

    // ── Register Begin ──────────────────────────────────────────────

    [Fact]
    public async Task RegisterBegin_WhenAuthenticated_Returns200WithJson()
    {
        var userId = await SeedTestUserAsync();
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

        var response = await client.PostAsync("/api/passkeys/register/begin", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterBegin_WhenUnauthenticated_Returns401()
    {
        var response = await _unauthenticated.PostAsync("/api/passkeys/register/begin", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Register Complete ─────────────────────────────────────────────

    [Fact]
    public async Task RegisterComplete_WhenUnauthenticated_Returns401()
    {
        using var content = new StringContent(
            """{"id":"test"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _unauthenticated.PostAsync("/api/passkeys/register/complete", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterComplete_WithInvalidCredential_ReturnsBadRequest()
    {
        var userId = await SeedTestUserAsync();
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, userId)
        );
        using var content = new StringContent(
            """{"invalid":"data"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync("/api/passkeys/register/complete", content);

        // Invalid attestation should be rejected
        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    // ── Login Begin ───────────────────────────────────────────────────

    [Fact]
    public async Task LoginBegin_WhenAnonymous_Returns200WithJson()
    {
        var response = await _unauthenticated.PostAsync("/api/passkeys/login/begin", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
    }

    // ── Login Complete ────────────────────────────────────────────────

    [Fact]
    public async Task LoginComplete_WithInvalidCredential_ReturnsUnauthorized()
    {
        using var content = new StringContent(
            """{"id":"invalid","type":"public-key"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _unauthenticated.PostAsync("/api/passkeys/login/complete", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginComplete_WithEmptyBody_ReturnsBadRequest()
    {
        var response = await _unauthenticated.PostAsync("/api/passkeys/login/complete", null);

        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    // ── Get Passkeys ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPasskeys_WhenUnauthenticated_Returns401()
    {
        var response = await _unauthenticated.GetAsync("/api/passkeys");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPasskeys_WhenAuthenticated_ReturnsOkWithList()
    {
        var userId = await SeedTestUserAsync();
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

        var response = await client.GetAsync("/api/passkeys");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNull();
        // New users have no passkeys — should return empty array
        body.Should().Be("[]");
    }

    // ── Delete Passkey ────────────────────────────────────────────────

    [Fact]
    public async Task DeletePasskey_WhenUnauthenticated_Returns401()
    {
        var response = await _unauthenticated.DeleteAsync("/api/passkeys/someCredentialId");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePasskey_WithNonExistentCredential_ReturnsNotFound()
    {
        var userId = await SeedTestUserAsync();
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

        // Use a valid base64url-encoded value that doesn't match any passkey
        var response = await client.DeleteAsync("/api/passkeys/dGVzdC1jcmVkZW50aWFsLWlk");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
