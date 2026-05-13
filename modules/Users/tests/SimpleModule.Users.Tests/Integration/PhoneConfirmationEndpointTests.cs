using System.Net;
using System.Net.Http;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public class PhoneConfirmationEndpointTests
{
    private const string SendCodePath = "/Identity/Account/Manage/SendPhoneVerificationCode";
    private const string ConfirmPath = "/Identity/Account/Manage/ConfirmPhoneNumber";
    private const string RemovePath = "/Identity/Account/Manage/RemovePhoneNumber";

    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly SimpleModuleWebApplicationFactory _factory;

    public PhoneConfirmationEndpointTests(SimpleModuleWebApplicationFactory factory)
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
            DisplayName = "Phone Test User",
        };
        var result = await userManager.CreateAsync(user, "TestPass1234!");
        result.Succeeded.Should().BeTrue();
        return (await userManager.FindByIdAsync(id))!;
    }

    private async Task<string> GenerateChangeTokenAsync(string userId, string phoneNumber)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        return await userManager.GenerateChangePhoneNumberTokenAsync(user!, phoneNumber);
    }

    private async Task<(string? phoneNumber, bool confirmed)> GetPhoneStateAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? (null, false) : (user.PhoneNumber, user.PhoneNumberConfirmed);
    }

    private async Task SetPhoneNumberDirectlyAsync(
        string userId,
        string phoneNumber,
        bool confirmed
    )
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        await userManager.SetPhoneNumberAsync(user!, phoneNumber);
        if (confirmed)
        {
            user!.PhoneNumberConfirmed = true;
            await userManager.UpdateAsync(user);
        }
    }

    [Fact]
    public async Task SendPhoneVerificationCode_WhenUnauthenticated_Returns401()
    {
        using var client = _factory.CreateClient(NoRedirect);
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("phoneNumber", "+15551234567"),
        ]);

        var response = await client.PostAsync(SendCodePath, form);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendPhoneVerificationCode_Authenticated_ReturnsOkAndLeavesPhoneUnconfirmed()
    {
        const string userId = "phone-send-user";
        await SeedUserAsync(userId);

        using var client = _factory.CreateAuthenticatedClient(
            NoRedirect,
            new Claim(ClaimTypes.NameIdentifier, userId)
        );
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("phoneNumber", "+15550001111"),
        ]);

        var response = await client.PostAsync(SendCodePath, form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (phone, confirmed) = await GetPhoneStateAsync(userId);
        phone.Should().BeNull("send-code should not save the phone number until verification");
        confirmed.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmPhoneNumber_WithValidCode_SetsPhoneAndConfirmsIt()
    {
        const string userId = "phone-confirm-happy-user";
        const string phoneNumber = "+15550001112";
        await SeedUserAsync(userId);
        var token = await GenerateChangeTokenAsync(userId, phoneNumber);

        using var client = _factory.CreateAuthenticatedClient(
            NoRedirect,
            new Claim(ClaimTypes.NameIdentifier, userId)
        );
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("phoneNumber", phoneNumber),
            new KeyValuePair<string, string>("code", token),
        ]);

        var response = await client.PostAsync(ConfirmPath, form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (phone, confirmed) = await GetPhoneStateAsync(userId);
        phone.Should().Be(phoneNumber);
        confirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmPhoneNumber_WithInvalidCode_DoesNotConfirm()
    {
        const string userId = "phone-confirm-bad-user";
        const string phoneNumber = "+15550001113";
        await SeedUserAsync(userId);

        using var client = _factory.CreateAuthenticatedClient(
            NoRedirect,
            new Claim(ClaimTypes.NameIdentifier, userId)
        );
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("phoneNumber", phoneNumber),
            new KeyValuePair<string, string>("code", "000000"),
        ]);

        var response = await client.PostAsync(ConfirmPath, form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (phone, confirmed) = await GetPhoneStateAsync(userId);
        phone.Should().BeNull("no save should occur when token is invalid");
        confirmed.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmPhoneNumber_ChangingNumberFromConfirmed_ResetsAndReconfirmsForNewNumber()
    {
        const string userId = "phone-change-user";
        const string originalPhone = "+15550002221";
        const string newPhone = "+15550002222";
        await SeedUserAsync(userId);
        await SetPhoneNumberDirectlyAsync(userId, originalPhone, confirmed: true);

        // sanity check
        var (preChangePhone, preConfirmed) = await GetPhoneStateAsync(userId);
        preChangePhone.Should().Be(originalPhone);
        preConfirmed.Should().BeTrue();

        var token = await GenerateChangeTokenAsync(userId, newPhone);
        using var client = _factory.CreateAuthenticatedClient(
            NoRedirect,
            new Claim(ClaimTypes.NameIdentifier, userId)
        );
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("phoneNumber", newPhone),
            new KeyValuePair<string, string>("code", token),
        ]);

        var response = await client.PostAsync(ConfirmPath, form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (phone, confirmed) = await GetPhoneStateAsync(userId);
        phone.Should().Be(newPhone);
        confirmed
            .Should()
            .BeTrue("verification of the new number replaces the previous confirmation atomically");
    }

    [Fact]
    public async Task RemovePhoneNumber_Authenticated_ClearsPhoneAndConfirmation()
    {
        const string userId = "phone-remove-user";
        await SeedUserAsync(userId);
        await SetPhoneNumberDirectlyAsync(userId, "+15550003333", confirmed: true);

        using var client = _factory.CreateAuthenticatedClient(
            NoRedirect,
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

        var response = await client.PostAsync(RemovePath, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (phone, confirmed) = await GetPhoneStateAsync(userId);
        phone.Should().BeNull();
        confirmed.Should().BeFalse();
    }

    [Fact]
    public async Task RemovePhoneNumber_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient(NoRedirect);

        var response = await client.PostAsync(RemovePath, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
