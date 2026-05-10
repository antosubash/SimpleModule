using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );
    }

    private sealed class RecordingUnlockEmailSender : IAccountUnlockEmailSender
    {
        public List<(string Email, string Link)> Calls { get; } = new();

        public Task SendUnlockLinkAsync(string email, string unlockLink)
        {
            Calls.Add((email, unlockLink));
            return Task.CompletedTask;
        }
    }

    private sealed class SendUnlockTestContext : IDisposable
    {
        public required WebApplicationFactory<Program> Factory { get; init; }
        public required HttpClient Client { get; init; }
        public required RecordingUnlockEmailSender Sender { get; init; }

        public void Dispose() => Client.Dispose();
    }

    private SendUnlockTestContext CreateTestContext()
    {
        var recorder = new RecordingUnlockEmailSender();
        var customFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAccountUnlockEmailSender>();
                services.AddSingleton<IAccountUnlockEmailSender>(recorder);
            })
        );
        return new SendUnlockTestContext
        {
            Factory = customFactory,
            Client = customFactory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
            ),
            Sender = recorder,
        };
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        WebApplicationFactory<Program> factory,
        string email,
        bool emailConfirmed,
        bool locked
    )
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            DisplayName = email,
        };
        await userManager.CreateAsync(user, "TestPass1234!");

        if (locked)
        {
            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));
        }

        return user;
    }

    [Fact]
    public async Task SendUnlockEmail_Get_RendersForm()
    {
        var response = await _client.GetAsync("/Identity/Account/SendUnlockEmail");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendUnlockEmailConfirmation_Get_RendersPage()
    {
        var response = await _client.GetAsync("/Identity/Account/SendUnlockEmailConfirmation");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendUnlockEmail_UnknownEmail_RedirectsAndDoesNotSend()
    {
        using var ctx = CreateTestContext();

        using var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("email", "nonexistent@example.com"),
        ]);

        var response = await ctx.Client.PostAsync("/Identity/Account/SendUnlockEmail", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response
            .Headers.Location!.OriginalString.Should()
            .Contain("/Identity/Account/SendUnlockEmailConfirmation");
        ctx.Sender.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task SendUnlockEmail_LockedUser_SendsUnlockLink()
    {
        using var ctx = CreateTestContext();

        var email = $"locked-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(ctx.Factory, email, emailConfirmed: true, locked: true);

        using var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("email", email),
        ]);

        var response = await ctx.Client.PostAsync("/Identity/Account/SendUnlockEmail", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response
            .Headers.Location!.OriginalString.Should()
            .Contain("/Identity/Account/SendUnlockEmailConfirmation");

        ctx.Sender.Calls.Should().ContainSingle();
        ctx.Sender.Calls[0].Email.Should().Be(email);
        ctx.Sender.Calls[0].Link.Should().Contain("/Identity/Account/UnlockAccount");
        ctx.Sender.Calls[0].Link.Should().Contain("userId=");
        ctx.Sender.Calls[0].Link.Should().Contain("code=");
    }

    [Fact]
    public async Task SendUnlockEmail_UnlockedUser_DoesNotSend()
    {
        using var ctx = CreateTestContext();

        var email = $"healthy-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(ctx.Factory, email, emailConfirmed: true, locked: false);

        using var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("email", email),
        ]);

        var response = await ctx.Client.PostAsync("/Identity/Account/SendUnlockEmail", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        ctx.Sender.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task SendUnlockEmail_UnconfirmedEmail_DoesNotSend()
    {
        using var ctx = CreateTestContext();

        var email = $"unconfirmed-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(ctx.Factory, email, emailConfirmed: false, locked: true);

        using var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("email", email),
        ]);

        var response = await ctx.Client.PostAsync("/Identity/Account/SendUnlockEmail", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        ctx.Sender.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task SendUnlockEmail_ExceedsRateLimit_Returns429()
    {
        using var ctx = CreateTestContext();

        // "auth-strict" allows 10 requests per minute per IP; the 11th must be 429.
        HttpStatusCode? lastStatus = null;
        for (var i = 0; i < 11; i++)
        {
            using var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("email", $"ratelimit-{i}@example.com"),
            ]);
            using var response = await ctx.Client.PostAsync(
                "/Identity/Account/SendUnlockEmail",
                content
            );
            lastStatus = response.StatusCode;
        }

        lastStatus.Should().Be(HttpStatusCode.TooManyRequests);
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
    public async Task UnlockAccount_MalformedBase64Code_ReturnsPage()
    {
        var user = await CreateUserAsync(
            _factory,
            $"malformed-{Guid.NewGuid():N}@example.com",
            emailConfirmed: true,
            locked: true
        );

        // "@@@" is not valid base64url and would throw FormatException without the guard.
        var response = await _client.GetAsync(
            $"/Identity/Account/UnlockAccount?userId={Uri.EscapeDataString(user.Id)}&code=%40%40%40"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnlockAccount_InvalidToken_ReturnsPage()
    {
        var user = await CreateUserAsync(
            _factory,
            $"invalid-token-{Guid.NewGuid():N}@example.com",
            emailConfirmed: true,
            locked: false
        );

        var tamperedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("tampered-token"));
        var response = await _client.GetAsync(
            $"/Identity/Account/UnlockAccount?userId={Uri.EscapeDataString(user.Id)}&code={Uri.EscapeDataString(tamperedCode)}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnlockAccount_ValidToken_UnlocksUser()
    {
        var email = $"valid-unlock-{Guid.NewGuid():N}@example.com";
        var user = await CreateUserAsync(_factory, email, emailConfirmed: true, locked: true);

        await using var setupScope = _factory.Services.CreateAsyncScope();
        var userManager = setupScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
        var code = await userManager.GenerateUserTokenAsync(
            user,
            TokenOptions.DefaultProvider,
            UsersConstants.TokenPurposes.AccountUnlock
        );
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var response = await _client.GetAsync(
            $"/Identity/Account/UnlockAccount?userId={Uri.EscapeDataString(user.Id)}&code={Uri.EscapeDataString(encodedCode)}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyManager = verifyScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
        var updatedUser = await verifyManager.FindByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        (await verifyManager.IsLockedOutAsync(updatedUser!)).Should().BeFalse();
        updatedUser!.AccessFailedCount.Should().Be(0);
    }
}
