using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public class SignOutEverywhereEndpointTests
{
    private const string EndpointPath = "/Identity/Account/Manage/SignOutEverywhere";

    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly SimpleModuleWebApplicationFactory _factory;

    public SignOutEverywhereEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<ApplicationUser> SeedUserAsync(string id)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByIdAsync(id);
        if (existing is not null)
            return existing;

        var user = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@example.com",
            Email = $"{id}@example.com",
            DisplayName = "Sign-out Test User",
        };
        var result = await userManager.CreateAsync(user, "TestPass1234!");
        result.Succeeded.Should().BeTrue();
        return (await userManager.FindByIdAsync(id))!;
    }

    private async Task<string?> GetSecurityStampAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        return user?.SecurityStamp;
    }

    [Fact]
    public async Task Post_WhenUnauthenticated_Returns401()
    {
        using var client = _factory.CreateClient(NoRedirect);

        var response = await client.PostAsync(EndpointPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WhenAuthenticated_RegeneratesSecurityStampAndRedirectsToLogin()
    {
        const string userId = "signout-everywhere-user";
        await SeedUserAsync(userId);
        var stampBefore = await GetSecurityStampAsync(userId);
        stampBefore.Should().NotBeNullOrEmpty();

        using var client = _factory.CreateAuthenticatedClient(
            NoRedirect,
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

        var response = await client.PostAsync(EndpointPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response
            .Headers.Location?.OriginalString.Should()
            .Contain("signedOutEverywhere=true", "user should land on login with the toast flag");

        var stampAfter = await GetSecurityStampAsync(userId);
        stampAfter
            .Should()
            .NotBeNullOrEmpty()
            .And.NotBe(
                stampBefore,
                "UpdateSecurityStampAsync must change the stamp so existing cookies/tokens "
                    + "stop validating on every other device once SecurityStampValidator runs."
            );
    }

    [Fact]
    public async Task Post_WhenUserDoesNotExist_RedirectsToLogin()
    {
        using var client = _factory.CreateAuthenticatedClient(
            NoRedirect,
            new Claim(ClaimTypes.NameIdentifier, "nonexistent-signout-user")
        );

        var response = await client.PostAsync(EndpointPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location?.OriginalString.Should().Contain("/Identity/Account/Login");
    }
}
