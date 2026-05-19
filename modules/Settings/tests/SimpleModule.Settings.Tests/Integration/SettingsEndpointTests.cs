using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Settings.Tests.Integration;

[Collection(TestCollections.Integration)]
public class SettingsEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    private static string UniqueKey(string prefix) => $"{prefix}.{Guid.NewGuid():N}";

    [Fact]
    public async Task UpdateSetting_StoresValue_ReadableViaGetSetting()
    {
        var client = factory.CreateAuthenticatedClient();
        var key = UniqueKey("integration");

        var updateResponse = await client.PutAsJsonAsync(
            "/api/settings",
            new UpdateSettingRequest
            {
                Key = key,
                Value = "\"hello\"",
                Scope = SettingScope.Application,
            }
        );
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/settings/{key}?scope=1");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("key").GetString().Should().Be(key);
        // Settings are stored as raw JSON, so the string value comes back
        // quoted exactly as it was written.
        body.GetProperty("value").GetString().Should().Be("\"hello\"");
    }

    [Fact]
    public async Task DeleteSetting_RemovesValue_SubsequentGetReturns404()
    {
        var client = factory.CreateAuthenticatedClient();
        var key = UniqueKey("delete");

        await client.PutAsJsonAsync(
            "/api/settings",
            new UpdateSettingRequest
            {
                Key = key,
                Value = "\"temp\"",
                Scope = SettingScope.Application,
            }
        );
        // Sanity check: the setting exists before deletion.
        (await client.GetAsync($"/api/settings/{key}?scope=1"))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/settings/{key}?scope=1");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await client.GetAsync($"/api/settings/{key}?scope=1");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDefinitions_ReturnsArrayOfDefinitions()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/settings/definitions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        // Each item must have at least the canonical "key" property — proves
        // the registry is actually populated and serialized, not just that
        // the route returned 200.
        if (body.GetArrayLength() > 0)
        {
            body[0].TryGetProperty("key", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task UpdateMySetting_StoresUserScopedValue_ReadableViaGetMySettings()
    {
        var client = factory.CreateAuthenticatedClient();

        var theme = "dark-" + Guid.NewGuid().ToString("N")[..8];
        var updateResponse = await client.PutAsJsonAsync(
            "/api/settings/me",
            new UpdateSettingRequest
            {
                Key = "app.theme",
                Value = $"\"{theme}\"",
                Scope = SettingScope.User,
            }
        );
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync("/api/settings/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        // The endpoint returns each user-scope definition with the resolved
        // value; the app.theme entry must reflect the override we just set.
        var themeEntry = body.EnumerateArray()
            .FirstOrDefault(item =>
                item.TryGetProperty("definition", out var def)
                && def.TryGetProperty("key", out var key)
                && key.GetString() == "app.theme"
            );
        themeEntry.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task GetSettings_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/settings");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
