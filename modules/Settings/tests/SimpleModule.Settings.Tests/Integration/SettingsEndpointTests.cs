using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Core.Settings;
using SimpleModule.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Settings.Tests.Integration;

[Collection(TestCollections.Integration)]
public class SettingsEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    private static string UniqueKey(string prefix) => $"{prefix}.{Guid.NewGuid():N}";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private HttpClient CreateAdminClient() =>
        factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

    private static UpdateSettingRequest StringRequest(
        string key,
        string value,
        SettingScope scope
    ) =>
        new()
        {
            Key = key,
            Scope = scope,
            Value = JsonSerializer.Deserialize<JsonElement>($"\"{value}\""),
        };

    [Fact]
    public async Task UpdateSetting_StoresValue_ReadableViaGetSetting()
    {
        var client = CreateAdminClient();
        var key = UniqueKey("integration");

        var updateResponse = await client.PutAsJsonAsync(
            "/api/settings",
            StringRequest(key, "hello", SettingScope.Application),
            JsonOptions
        );
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/settings/{key}?scope=1");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("key").GetString().Should().Be(key);
        body.GetProperty("isOverridden").GetBoolean().Should().BeTrue();
        body.GetProperty("value").GetString().Should().Be("hello");
    }

    [Fact]
    public async Task DeleteSetting_RemovesValue_SubsequentGetReturns404()
    {
        var client = CreateAdminClient();
        var key = UniqueKey("delete");

        await client.PutAsJsonAsync(
            "/api/settings",
            StringRequest(key, "temp", SettingScope.Application),
            JsonOptions
        );
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
        if (body.GetArrayLength() > 0)
        {
            body[0].TryGetProperty("key", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task BulkUpdateSettings_StoresMultipleValues()
    {
        var client = CreateAdminClient();
        var key1 = UniqueKey("bulk1");
        var key2 = UniqueKey("bulk2");

        var request = new BulkUpdateSettingsRequest
        {
            Updates =
            [
                new BulkSettingUpdate
                {
                    Key = key1,
                    Scope = SettingScope.Application,
                    Value = JsonSerializer.Deserialize<JsonElement>("\"val1\""),
                },
                new BulkSettingUpdate
                {
                    Key = key2,
                    Scope = SettingScope.Application,
                    Value = JsonSerializer.Deserialize<JsonElement>("\"val2\""),
                },
            ],
        };

        var response = await client.PutAsJsonAsync("/api/settings/bulk", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("count").GetInt32().Should().Be(2);

        var get1 = await client.GetAsync($"/api/settings/{key1}?scope=1");
        get1.StatusCode.Should().Be(HttpStatusCode.OK);
        var get2 = await client.GetAsync($"/api/settings/{key2}?scope=1");
        get2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // BUG-5: PUT /api/settings/bulk with a User-scope entry must return 400, not 500.
    [Fact]
    public async Task BulkUpdateSettings_WithUserScope_Returns400()
    {
        var client = factory.CreateAuthenticatedClient();

        var request = new BulkUpdateSettingsRequest
        {
            Updates =
            [
                new BulkSettingUpdate
                {
                    Key = "app.theme",
                    Scope = SettingScope.User,
                    Value = JsonSerializer.Deserialize<JsonElement>("\"dark\""),
                },
            ],
        };

        var response = await client.PutAsJsonAsync("/api/settings/bulk", request, JsonOptions);
        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.BadRequest,
                "User-scope entries in a bulk request must return 400 with a useful message, not 500"
            );
    }

    [Fact]
    public async Task UpdateMySetting_StoresUserScopedValue_ReadableViaGetMySettings()
    {
        var client = factory.CreateAuthenticatedClient();

        var theme = "dark-" + Guid.NewGuid().ToString("N")[..8];
        var updateResponse = await client.PutAsJsonAsync(
            "/api/settings/me",
            StringRequest("app.theme", theme, SettingScope.User),
            JsonOptions
        );
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync("/api/settings/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        var themeEntry = body.EnumerateArray()
            .FirstOrDefault(item =>
                item.TryGetProperty("key", out var k) && k.GetString() == "app.theme"
            );
        themeEntry.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        themeEntry.GetProperty("isOverridden").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetResolvedSetting_ReturnsEffectiveValue()
    {
        var client = factory.CreateAuthenticatedClient();
        var key = UniqueKey("resolved");

        var response = await client.GetAsync($"/api/settings/{key}/resolved");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("key").GetString().Should().Be(key);
    }

    [Fact]
    public async Task GetSettings_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/settings");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // GAP-PERM: write endpoints require Settings.Update permission

    [Fact]
    public async Task UpdateSetting_WithoutPermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync(
            "/api/settings",
            StringRequest(UniqueKey("perm"), "value", SettingScope.Application),
            JsonOptions
        );
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteSetting_WithoutPermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/settings/some.key?scope=1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BulkUpdateSettings_WithoutPermission_Returns403()
    {
        var client = factory.CreateAuthenticatedClient();
        var request = new BulkUpdateSettingsRequest
        {
            Updates =
            [
                new BulkSettingUpdate
                {
                    Key = UniqueKey("bulk-perm"),
                    Scope = SettingScope.Application,
                    Value = JsonSerializer.Deserialize<JsonElement>("\"v\""),
                },
            ],
        };
        var response = await client.PutAsJsonAsync("/api/settings/bulk", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // BUG-3: GET /api/settings/{key} without ?scope returns 400

    [Fact]
    public async Task GetSetting_WithoutScope_Returns400()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/settings/any.key");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // BUG-4: DELETE /api/settings/{key} without ?scope returns 400

    [Fact]
    public async Task DeleteSetting_WithoutScope_Returns400()
    {
        var client = CreateAdminClient();
        var response = await client.DeleteAsync("/api/settings/any.key");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // BUG-6: PUT /api/settings with scope=User returns 400

    [Fact]
    public async Task UpdateSetting_WithUserScope_Returns400()
    {
        var client = CreateAdminClient();
        var response = await client.PutAsJsonAsync(
            "/api/settings",
            StringRequest(UniqueKey("user-scope"), "value", SettingScope.User),
            JsonOptions
        );
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // BUG-6: PUT /api/settings/bulk with any User-scope entry returns 400

    [Fact]
    public async Task BulkUpdateSettings_WithUserScopeEntry_Returns400()
    {
        var client = CreateAdminClient();
        var request = new BulkUpdateSettingsRequest
        {
            Updates =
            [
                new BulkSettingUpdate
                {
                    Key = UniqueKey("bulk-user"),
                    Scope = SettingScope.User,
                    Value = JsonSerializer.Deserialize<JsonElement>("\"v\""),
                },
            ],
        };
        var response = await client.PutAsJsonAsync("/api/settings/bulk", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
