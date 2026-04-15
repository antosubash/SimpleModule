using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users;
using SimpleModule.Users.Contracts;

namespace SimpleModule.LoadTests.Infrastructure;

public partial class LoadTestWebApplicationFactory
{
    private async Task SeedTestInfrastructureAsync()
    {
        if (_seeded)
            return;
        _seeded = true;

        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        // 1. Seed the ROPC-capable OAuth client
        var appManager = sp.GetRequiredService<IOpenIddictApplicationManager>();
        if (await appManager.FindByClientIdAsync(ServiceClientId) is null)
        {
            await appManager.CreateAsync(
                new OpenIddictApplicationDescriptor
                {
                    ClientId = ServiceClientId,
                    ClientSecret = ServiceClientSecret,
                    DisplayName = "Load Test Service Account",
                    ClientType = OpenIddictConstants.ClientTypes.Confidential,
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.Password,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Prefixes.Scope + AuthConstants.RolesScope,
                    },
                }
            );
        }

        // 2. Seed the Admin role
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(
                new ApplicationRole
                {
                    Name = "Admin",
                    Description = "Load test admin role",
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }

        // 3. Seed the test user
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(TestUserEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = TestUserEmail,
                Email = TestUserEmail,
                DisplayName = TestUserDisplayName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
            };
            var result = await userManager.CreateAsync(user, TestUserPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        // 4. Assign all permissions to the user
        var permContracts = sp.GetRequiredService<IPermissionContracts>();
        var userId = UserId.From(user!.Id);
        await permContracts.SetPermissionsForUserAsync(userId, AllPermissions);
    }

    /// <summary>
    /// Acquires a real Bearer token via ROPC (password grant) from /connect/token.
    /// The token contains real claims: sub, email, name, roles, permissions.
    /// </summary>
    public async Task<HttpClient> CreateBearerClientAsync()
    {
        await SeedTestInfrastructureAsync();

        using var tokenClient = CreateClient();
        using var tokenRequest = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", ServiceClientId),
            new KeyValuePair<string, string>("client_secret", ServiceClientSecret),
            new KeyValuePair<string, string>("username", TestUserEmail),
            new KeyValuePair<string, string>("password", TestUserPassword),
            new KeyValuePair<string, string>("scope", "openid profile email roles"),
        ]);

        var tokenResponse = await tokenClient.PostAsync("/connect/token", tokenRequest);
        var body = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to acquire Bearer token: {tokenResponse.StatusCode} - {body}"
            );
        }

        using var doc = JsonDocument.Parse(body);
        var accessToken =
            doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Token response missing access_token");

        var client = CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            }
        );
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        return client;
    }

    /// <summary>
    /// Gets the seeded test user's Identity ID.
    /// </summary>
    public async Task<string> GetSeededUserIdAsync()
    {
        await SeedTestInfrastructureAsync();

        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user =
            await userManager.FindByEmailAsync(TestUserEmail)
            ?? throw new InvalidOperationException("Seeded user not found");
        return user.Id;
    }
}
