using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Testing;
using SimpleModule.Users.Contracts;

namespace OpenIddict.Tests.Integration;

[Collection(TestCollections.Integration)]
public class ActiveSessionsEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ActiveSessionsEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static WebApplicationFactoryClientOptions NoRedirects() =>
        new() { AllowAutoRedirect = false };

    private async Task<string> SeedUserAsync(string idHint)
    {
        // Ensures module databases are created (the factory's instance method
        // does this lazily on first use). Required before resolving UserManager
        // because the OpenIddict.Tests project doesn't share the Users.Tests
        // seeding path.
        using (_factory.CreateAuthenticatedClient()) { }

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userId = $"active-sessions-{idHint}";
        var existing = await userManager.FindByIdAsync(userId);
        if (existing is not null)
            return userId;

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"{userId}@example.com",
            Email = $"{userId}@example.com",
            DisplayName = $"Test User {idHint}",
        };
        await userManager.CreateAsync(user, "TestPass1234!");
        return userId;
    }

    private HttpClient CreateAuthenticatedNoRedirectClient(string userId)
    {
        var client = _factory.CreateClient(NoRedirects());
        client.DefaultRequestHeaders.Add(
            TestAuthDefaults.ClaimsHeader,
            $"{ClaimTypes.NameIdentifier}={userId}"
        );
        return client;
    }

    // ── GET page ───────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WhenAuthenticated_Returns200()
    {
        var userId = await SeedUserAsync("get-1");
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

        var response = await client.GetAsync("/Identity/Account/Manage/ActiveSessions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WhenUnauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(NoRedirects());

        var response = await client.GetAsync("/Identity/Account/Manage/ActiveSessions");

        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    // ── POST revoke single ─────────────────────────────────────────────

    [Fact]
    public async Task Revoke_WhenUnauthenticated_RedirectsOrUnauthorized()
    {
        var client = _factory.CreateClient(NoRedirects());

        var response = await client.PostAsync(
            "/Identity/Account/Manage/ActiveSessions/some-token/revoke",
            content: null
        );

        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    [Fact]
    public async Task Revoke_WhenSessionDoesNotBelongToCaller_Returns404()
    {
        // The shared in-memory database has no OpenIddict tokens for this user, so
        // any token id is treated as "not owned by the caller" → 404. Defends
        // against the cross-user attack where an attacker guesses someone else's
        // token id.
        var userId = await SeedUserAsync("revoke-1");
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

        var response = await client.PostAsync(
            "/Identity/Account/Manage/ActiveSessions/someone-elses-token-id/revoke",
            content: null
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST revoke-others ─────────────────────────────────────────────

    [Fact]
    public async Task RevokeOthers_WhenUnauthenticated_RedirectsOrUnauthorized()
    {
        var client = _factory.CreateClient(NoRedirects());

        var response = await client.PostAsync(
            "/Identity/Account/Manage/ActiveSessions/revoke-others",
            content: null
        );

        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    [Fact]
    public async Task RevokeOthers_WhenAuthenticated_RedirectsToListing()
    {
        var userId = await SeedUserAsync("revoke-others-1");
        using var client = CreateAuthenticatedNoRedirectClient(userId);

        var response = await client.PostAsync(
            "/Identity/Account/Manage/ActiveSessions/revoke-others",
            content: null
        );

        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.Headers.Location?.ToString()
            .Should()
            .Contain("/Identity/Account/Manage/ActiveSessions");
    }
}
