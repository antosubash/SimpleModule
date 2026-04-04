using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Settings.Tests.Integration;

[Collection(TestCollections.Integration)]
public class SettingsEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    [Fact]
    public async Task GetDefinitions_Authenticated_Returns200()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/settings/definitions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSetting_Authenticated_Returns204()
    {
        var client = factory.CreateAuthenticatedClient();
        var request = new UpdateSettingRequest
        {
            Key = "test.key",
            Value = "\"test-value\"",
            Scope = SettingScope.Application,
        };
        var response = await client.PutAsJsonAsync("/api/settings", request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetSetting_AfterUpdate_ReturnsValue()
    {
        var client = factory.CreateAuthenticatedClient();
        await client.PutAsJsonAsync(
            "/api/settings",
            new UpdateSettingRequest
            {
                Key = "integration.test",
                Value = "\"hello\"",
                Scope = SettingScope.Application,
            }
        );
        var response = await client.GetAsync("/api/settings/integration.test?scope=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteSetting_Authenticated_Returns204()
    {
        var client = factory.CreateAuthenticatedClient();
        await client.PutAsJsonAsync(
            "/api/settings",
            new UpdateSettingRequest
            {
                Key = "delete.test",
                Value = "\"temp\"",
                Scope = SettingScope.Application,
            }
        );
        var response = await client.DeleteAsync("/api/settings/delete.test?scope=1");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetMySettings_Authenticated_Returns200()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/settings/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateMySetting_Authenticated_Returns204()
    {
        var client = factory.CreateAuthenticatedClient();
        var request = new UpdateSettingRequest
        {
            Key = "app.theme",
            Value = "\"dark\"",
            Scope = SettingScope.User,
        };
        var response = await client.PutAsJsonAsync("/api/settings/me", request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetSettings_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/settings");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
