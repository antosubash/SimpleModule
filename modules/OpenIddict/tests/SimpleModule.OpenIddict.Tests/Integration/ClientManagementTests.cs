using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using SimpleModule.Tests.Shared.Fixtures;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.Tests.Integration;

[Collection("Integration")]
public class ClientManagementTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ClientManagementTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

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

    private async Task<string> SeedTestClientAsync(string? clientId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var id = clientId ?? $"test-client-{Guid.NewGuid():N}";
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = id,
            DisplayName = $"Test Client {id}",
            ClientType = ClientTypes.Public,
        };

        var app = await manager.CreateAsync(descriptor);
        return (await manager.GetIdAsync(app))!;
    }

    // ── View endpoint tests ──────────────────────────────────────────

    [Fact]
    public async Task GetClients_AsAdmin_Returns200()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/openiddict/clients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClients_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var response = await client.GetAsync("/openiddict/clients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClientsCreate_AsAdmin_Returns200()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/openiddict/clients/create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClientsEdit_ExistingClient_Returns200()
    {
        var appId = await SeedTestClientAsync();
        var client = CreateAdminClient();

        var response = await client.GetAsync($"/openiddict/clients/{appId}/edit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClientsEdit_NonExistentClient_Returns404()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/openiddict/clients/nonexistent-id/edit");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Create tests ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateClient_ValidData_Redirects()
    {
        var client = CreateAdminClient();
        var newClientId = $"public-{Guid.NewGuid():N}";

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["clientId"] = newClientId,
                ["displayName"] = "Public Test Client",
                ["clientType"] = ClientTypes.Public,
            }
        );

        var response = await client.PostAsync("/openiddict/clients", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/openiddict/clients/");
        response.Headers.Location?.ToString().Should().Contain("/edit");
    }

    [Fact]
    public async Task CreateClient_ConfidentialType_Redirects()
    {
        var client = CreateAdminClient();
        var newClientId = $"confidential-{Guid.NewGuid():N}";

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["clientId"] = newClientId,
                ["displayName"] = "Confidential Test Client",
                ["clientType"] = ClientTypes.Confidential,
                ["clientSecret"] = "super-secret-value-123",
            }
        );

        var response = await client.PostAsync("/openiddict/clients", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/openiddict/clients/");
        response.Headers.Location?.ToString().Should().Contain("/edit");
    }

    [Fact]
    public async Task CreateClient_CanBeRetrievedAfterCreation()
    {
        var client = CreateAdminClient();
        var newClientId = $"retrieve-{Guid.NewGuid():N}";

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["clientId"] = newClientId,
                ["displayName"] = "Retrievable Client",
                ["clientType"] = ClientTypes.Public,
            }
        );

        await client.PostAsync("/openiddict/clients", content);

        using var scope = _factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var application = await manager.FindByClientIdAsync(newClientId);
        application.Should().NotBeNull();

        var displayName = await manager.GetDisplayNameAsync(application!);
        displayName.Should().Be("Retrievable Client");
    }

    // ── Update tests ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateClient_Details_Redirects()
    {
        var appId = await SeedTestClientAsync();
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["displayName"] = "Updated Display Name",
                ["clientType"] = ClientTypes.Public,
            }
        );

        var response = await client.PostAsync($"/openiddict/clients/{appId}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain($"/openiddict/clients/{appId}/edit");
    }

    [Fact]
    public async Task UpdateClient_URIs_Redirects()
    {
        var appId = await SeedTestClientAsync();
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("redirectUris", "https://example.com/callback"),
                new KeyValuePair<string, string>("postLogoutUris", "https://example.com/logout"),
            }
        );

        var response = await client.PostAsync($"/openiddict/clients/{appId}/uris", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain($"/openiddict/clients/{appId}/edit");
    }

    [Fact]
    public async Task UpdateClient_Permissions_Redirects()
    {
        var appId = await SeedTestClientAsync();
        var client = CreateAdminClient();

        using var content = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>(
                    "permissions",
                    Permissions.Endpoints.Authorization
                ),
                new KeyValuePair<string, string>("permissions", Permissions.Endpoints.Token),
            }
        );

        var response = await client.PostAsync($"/openiddict/clients/{appId}/permissions", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain($"/openiddict/clients/{appId}/edit");
    }

    // ── Delete tests ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteClient_ExistingClient_Redirects()
    {
        var appId = await SeedTestClientAsync();
        var client = CreateAdminClient();

        var response = await client.DeleteAsync($"/openiddict/clients/{appId}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Be("/openiddict/clients");
    }

    [Fact]
    public async Task DeleteClient_NonExistentClient_Returns404()
    {
        var client = CreateAdminClient();

        var response = await client.DeleteAsync("/openiddict/clients/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteClient_VerifiesRemoval()
    {
        var clientId = $"delete-verify-{Guid.NewGuid():N}";
        var appId = await SeedTestClientAsync(clientId);
        var client = CreateAdminClient();

        await client.DeleteAsync($"/openiddict/clients/{appId}");

        using var scope = _factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var application = await manager.FindByClientIdAsync(clientId);
        application.Should().BeNull();
    }
}
