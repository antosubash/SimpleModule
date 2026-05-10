using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public class AccountUnlockEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountUnlockEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            }
        );
    }

    [Fact]
    public async Task SendUnlockEmail_UnknownEmail_RedirectsToConfirmation()
    {
        using var content = new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("email", "nonexistent@example.com")]
        );

        var response = await _client.PostAsync(
            "/Identity/Account/SendUnlockEmail",
            content
        );

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should()
            .Contain("/Identity/Account/SendUnlockEmailConfirmation");
    }

    [Fact]
    public async Task SendUnlockEmail_LockedUser_RedirectsToConfirmation()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "locked-unlock-test@example.com",
            Email = "locked-unlock-test@example.com",
            EmailConfirmed = true,
            DisplayName = "Locked User",
        };
        await userManager.CreateAsync(user, "TestPass1234!");
        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(
            user,
            DateTimeOffset.UtcNow.AddHours(1)
        );

        using var content = new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("email", user.Email)]
        );

        var response = await _client.PostAsync(
            "/Identity/Account/SendUnlockEmail",
            content
        );

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should()
            .Contain("/Identity/Account/SendUnlockEmailConfirmation");
    }

    [Fact]
    public async Task UnlockAccount_MissingParams_RedirectsToHome()
    {
        var response = await _client.GetAsync("/Identity/Account/UnlockAccount");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be("/");
    }

    [Fact]
    public async Task UnlockAccount_InvalidUserId_ReturnsPage()
    {
        var response = await _client.GetAsync(
            "/Identity/Account/UnlockAccount?userId=nonexistent&code=bogus"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnlockAccount_InvalidToken_ReturnsPage()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "invalid-token-test@example.com",
            Email = "invalid-token-test@example.com",
            EmailConfirmed = true,
            DisplayName = "Token Test User",
        };
        await userManager.CreateAsync(user, "TestPass1234!");

        var tamperedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("tampered-token"));
        var response = await _client.GetAsync(
            $"/Identity/Account/UnlockAccount?userId={Uri.EscapeDataString(user.Id)}&code={Uri.EscapeDataString(tamperedCode)}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnlockAccount_ValidToken_UnlocksUser()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "valid-unlock-test@example.com",
            Email = "valid-unlock-test@example.com",
            EmailConfirmed = true,
            DisplayName = "Valid Unlock User",
        };
        await userManager.CreateAsync(user, "TestPass1234!");
        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(
            user,
            DateTimeOffset.UtcNow.AddHours(1)
        );

        // Generate a valid unlock token
        var code = await userManager.GenerateUserTokenAsync(
            user,
            TokenOptions.DefaultProvider,
            "AccountUnlock"
        );
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var response = await _client.GetAsync(
            $"/Identity/Account/UnlockAccount?userId={Uri.EscapeDataString(user.Id)}&code={Uri.EscapeDataString(encodedCode)}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the user is actually unlocked
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyManager =
            verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await verifyManager.FindByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        (await verifyManager.IsLockedOutAsync(updatedUser!)).Should().BeFalse();
        updatedUser!.AccessFailedCount.Should().Be(0);
    }
}
