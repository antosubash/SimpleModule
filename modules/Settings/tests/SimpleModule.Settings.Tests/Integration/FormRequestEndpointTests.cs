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
public class FormRequestEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    private static string UniqueKey(string prefix) => $"{prefix}.k{Guid.NewGuid():N}";

    // ─── UpdateSetting: valid requests ────────────────────────────────────────

    [Fact]
    public async Task UpdateSetting_ValidRequest_Returns204()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = UniqueKey("test.formrequest"),
                Value = JsonSerializer.Deserialize<JsonElement>("\"hello\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateSetting_NullJsonValue_Returns204()
    {
        // Value is JsonElement — a JSON null is a valid JsonElement with ValueKind.Null.
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = UniqueKey("test.nullvalue"),
                Value = JsonSerializer.Deserialize<JsonElement>("null"),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateSetting_KeyWithWhitespace_TrimsAndSucceeds()
    {
        // Prepare() should trim whitespace from the key before validation runs.
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);
        var baseKey = UniqueKey("test.trimmed");

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = $"  {baseKey}  ",
                Value = JsonSerializer.Deserialize<JsonElement>("\"trimmed\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ─── UpdateSetting: validation failures → 422 ─────────────────────────────

    [Fact]
    public async Task UpdateSetting_EmptyKey_Returns422WithKeyError()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = "",
                Value = JsonSerializer.Deserialize<JsonElement>("\"test\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertProblemHasFieldError(response, "Key");
    }

    [Fact]
    public async Task UpdateSetting_CamelCaseKey_Returns204()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = "test.camelCaseKey",
                Value = JsonSerializer.Deserialize<JsonElement>("\"test\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateSetting_TrailingDot_Returns422()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = "app.",
                Value = JsonSerializer.Deserialize<JsonElement>("\"test\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertProblemHasFieldError(response, "Key");
    }

    [Fact]
    public async Task UpdateSetting_InvalidKeyFormat_Returns422()
    {
        // "INVALID KEY!" contains spaces and special chars — should fail even after trim.
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = "INVALID KEY!",
                Value = JsonSerializer.Deserialize<JsonElement>("\"test\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertProblemHasFieldError(response, "Key");
    }

    [Fact]
    public async Task UpdateSetting_KeyTooLong_Returns422()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);
        // MaximumLength(256) means 256 is valid but 257 is not.
        var longKey = "a." + new string('a', 256);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = longKey,
                Value = JsonSerializer.Deserialize<JsonElement>("\"test\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertProblemHasFieldError(response, "Key");
    }

    [Fact]
    public async Task UpdateSetting_InvalidScopeEnum_Returns422()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

        // Use raw JSON to send scope=99 which is not a valid SettingScope value.
        using var content = new StringContent(
            """{"Key":"test.scope","Value":"x","Scope":99}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await client.PutAsync("/api/settings", content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertProblemHasFieldError(response, "Scope");
    }

    // ─── CreateMenuItem: valid requests ───────────────────────────────────────

    [Fact]
    public async Task CreateMenuItem_ValidRequest_Returns201WithLocation()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);

        var response = await client.PostAsJsonAsync(
            "/api/settings/menus",
            new
            {
                Label = $"Test Menu {Guid.NewGuid():N}",
                Url = "/test-page",
                IsVisible = true,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/settings/menus/");
    }

    [Fact]
    public async Task CreateMenuItem_MinimalRequest_Returns201()
    {
        // Only Label is required; all other fields are optional.
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);

        var response = await client.PostAsJsonAsync(
            "/api/settings/menus",
            new { Label = "Minimal Item" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateMenuItem_LabelTrimmedByPrepare_EntityHasTrimmedLabel()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);

        var response = await client.PostAsJsonAsync(
            "/api/settings/menus",
            new { Label = "  Trimmed Label  ", IsVisible = true }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Read back the created entity to verify the label was trimmed.
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("label").GetString().Should().Be("Trimmed Label");
    }

    // ─── CreateMenuItem: validation failures → 422 ────────────────────────────

    [Fact]
    public async Task CreateMenuItem_EmptyLabel_Returns422()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);

        var response = await client.PostAsJsonAsync(
            "/api/settings/menus",
            new { Label = "", IsVisible = true }
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertProblemHasFieldError(response, "Label");
    }

    [Fact]
    public async Task CreateMenuItem_LabelTooLong_Returns422()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);

        var response = await client.PostAsJsonAsync(
            "/api/settings/menus",
            new { Label = new string('X', 201), IsVisible = true }
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertProblemHasFieldError(response, "Label");
    }

    [Fact]
    public async Task CreateMenuItem_UrlTooLong_Returns422()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);

        var response = await client.PostAsJsonAsync(
            "/api/settings/menus",
            new
            {
                Label = "Valid Label",
                Url = "/" + new string('u', 2001),
                IsVisible = true,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertProblemHasFieldError(response, "Url");
    }

    // ─── Cross-cutting: RFC 7807 shape ────────────────────────────────────────

    [Fact]
    public async Task ValidationError_ResponseHasCorrectRfc7807Shape()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = "",
                Value = JsonSerializer.Deserialize<JsonElement>("\"test\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // RFC 7807 required fields
        body.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetInt32().Should().Be(422);

        body.TryGetProperty("title", out var titleProp).Should().BeTrue();
        titleProp.GetString().Should().Be("Validation Error");

        // The errors extension must be a dictionary of field → string[]
        body.TryGetProperty("errors", out var errorsProp).Should().BeTrue();
        errorsProp.ValueKind.Should().Be(JsonValueKind.Object);

        // Each error entry should be an array of strings
        foreach (var prop in errorsProp.EnumerateObject())
        {
            prop.Value.ValueKind.Should().Be(JsonValueKind.Array);
            foreach (var msg in prop.Value.EnumerateArray())
            {
                msg.ValueKind.Should().Be(JsonValueKind.String);
            }
        }
    }

    // ─── Cross-cutting: authentication before validation ──────────────────────

    [Fact]
    public async Task UpdateSetting_Unauthenticated_Returns401NotValidationError()
    {
        // Auth middleware must run before the FormRequest filter.
        // Even with invalid data, an unauthenticated request gets 401.
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                Key = "",
                Value = JsonSerializer.Deserialize<JsonElement>("\"test\""),
                Scope = SettingScope.Application,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateMenuItem_Unauthenticated_Returns401NotValidationError()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/settings/menus",
            new { Label = "", IsVisible = true }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static async Task AssertProblemHasFieldError(
        HttpResponseMessage response,
        string fieldName
    )
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.TryGetProperty("status", out var statusProp)
            .Should()
            .BeTrue("response should contain 'status'");
        statusProp.GetInt32().Should().Be(422);

        body.TryGetProperty("title", out var titleProp)
            .Should()
            .BeTrue("response should contain 'title'");
        titleProp.GetString().Should().Be("Validation Error");

        body.TryGetProperty("errors", out var errorsProp)
            .Should()
            .BeTrue("response should contain 'errors' dictionary");
        errorsProp
            .TryGetProperty(fieldName, out _)
            .Should()
            .BeTrue($"errors should contain key '{fieldName}'");
    }
}
