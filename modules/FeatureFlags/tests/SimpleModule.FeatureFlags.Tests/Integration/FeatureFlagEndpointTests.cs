using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.FeatureFlags;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace FeatureFlags.Tests.Integration;

public class FeatureFlagEndpointTests(SimpleModuleWebApplicationFactory factory)
    : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private static readonly string[] ViewPermission = [FeatureFlagsPermissions.View];

    private static readonly string[] AllPermissions =
    [
        FeatureFlagsPermissions.View,
        FeatureFlagsPermissions.Manage,
    ];

    [Fact]
    public async Task GetAllFlags_WithPermission_Returns200()
    {
        var client = factory.CreateAuthenticatedClient(ViewPermission);
        var response = await client.GetAsync("/api/feature-flags");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllFlags_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/feature-flags");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllFlags_NoPermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/feature-flags");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateFlag_WithPermission_Returns200()
    {
        var client = factory.CreateAuthenticatedClient(AllPermissions);

        // Create/update a flag directly by name
        var request = new UpdateFeatureFlagRequest { IsEnabled = true };
        var response = await client.PutAsJsonAsync(
            "/api/feature-flags/Integration.TestFlag",
            request
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CheckFlag_Authenticated_Returns200()
    {
        var client = factory.CreateAuthenticatedClient(ViewPermission);
        var response = await client.GetAsync("/api/feature-flags/check/nonexistent.flag");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 but got {(int)response.StatusCode}. Body: {body}"
        );
    }

    [Fact]
    public async Task SetOverride_WithPermission_Returns201()
    {
        var client = factory.CreateAuthenticatedClient(AllPermissions);

        // Ensure flag exists first
        await client.PutAsJsonAsync(
            "/api/feature-flags/Integration.OverrideTest",
            new UpdateFeatureFlagRequest { IsEnabled = false }
        );

        var request = new SetOverrideRequest
        {
            OverrideType = OverrideType.User,
            OverrideValue = "test-user-id",
            IsEnabled = true,
        };
        var response = await client.PostAsJsonAsync(
            "/api/feature-flags/Integration.OverrideTest/overrides",
            request
        );
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DeleteOverride_WithPermission_Returns204()
    {
        var client = factory.CreateAuthenticatedClient(AllPermissions);

        // Ensure flag exists first
        await client.PutAsJsonAsync(
            "/api/feature-flags/Integration.DeleteTest",
            new UpdateFeatureFlagRequest { IsEnabled = false }
        );

        var setRequest = new SetOverrideRequest
        {
            OverrideType = OverrideType.Role,
            OverrideValue = "test-role-delete",
            IsEnabled = true,
        };
        var setResponse = await client.PostAsJsonAsync(
            "/api/feature-flags/Integration.DeleteTest/overrides",
            setRequest
        );
        setResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await setResponse.Content.ReadFromJsonAsync<FeatureFlagOverride>();

        created.Should().NotBeNull();
        var deleteResponse = await client.DeleteAsync(
            $"/api/feature-flags/overrides/{created!.Id}"
        );
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
