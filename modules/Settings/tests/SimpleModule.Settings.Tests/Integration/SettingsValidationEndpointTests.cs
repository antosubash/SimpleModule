using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Core.Settings;
using SimpleModule.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Settings.Tests.Integration;

/// <summary>
/// Integration tests that hit the running ASP.NET pipeline to verify:
/// - Validation returns 400 (not 500) for type mismatches
/// - Endpoint binding errors return 400 (not 500) for missing query params
/// - BulkUpdate with User scope returns 400 (not 500)
/// - Decoded-value round-trips for all primitive types
/// - Cache invalidation after writes and deletes
/// - Scope isolation (userId-specific rows)
/// </summary>
[Collection(TestCollections.Integration)]
public class SettingsValidationEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    private static string UniqueKey(string prefix) => $"qa.{prefix}.{Guid.NewGuid():N}";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private HttpClient AdminClient() =>
        factory.CreateAuthenticatedClient([SettingsPermissions.Update]);

    // -----------------------------------------------------------------------
    // Decoded-value round-trips
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RoundTrip_BoolTrue_DecodedAsRealBool()
    {
        var client = AdminClient();
        var key = UniqueKey("bool");

        var put = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.Application,
                value = true,
            },
            JsonOptions
        );
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await client.GetAsync($"/api/settings/{key}?scope=1");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await get.Content.ReadFromJsonAsync<JsonElement>();
        // value must be JSON true (ValueKind.True), not the string "true"
        body.GetProperty("value").ValueKind.Should().Be(JsonValueKind.True);
    }

    [Fact]
    public async Task RoundTrip_Number_DecodedAsNumeric()
    {
        var client = AdminClient();
        var key = UniqueKey("num");

        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.Application,
                value = 42,
            },
            JsonOptions
        );

        var body = await (
            await client.GetAsync($"/api/settings/{key}?scope=1")
        ).Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("value").ValueKind.Should().Be(JsonValueKind.Number);
        body.GetProperty("value").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task RoundTrip_String_DecodedAsPlainString_NotDoubleEncoded()
    {
        var client = AdminClient();
        var key = UniqueKey("str");

        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.Application,
                value = "hello world",
            },
            JsonOptions
        );

        var body = await (
            await client.GetAsync($"/api/settings/{key}?scope=1")
        ).Content.ReadFromJsonAsync<JsonElement>();

        var value = body.GetProperty("value");
        value.ValueKind.Should().Be(JsonValueKind.String);
        // Must be the plain string, never a JSON-encoded string-within-a-string
        value.GetString().Should().Be("hello world");
        value.GetString().Should().NotStartWith("\"");
    }

    [Fact]
    public async Task RoundTrip_JsonObject_PreservesStructure()
    {
        var client = AdminClient();
        var key = UniqueKey("json");

        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.Application,
                value = new { nested = "object", count = 3 },
            },
            JsonOptions
        );

        var body = await (
            await client.GetAsync($"/api/settings/{key}?scope=1")
        ).Content.ReadFromJsonAsync<JsonElement>();

        var value = body.GetProperty("value");
        value.ValueKind.Should().Be(JsonValueKind.Object);
        value.GetProperty("nested").GetString().Should().Be("object");
        value.GetProperty("count").GetInt32().Should().Be(3);
    }

    // -----------------------------------------------------------------------
    // Validation returns 400
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validation_BoolAString_Returns400WithFieldError()
    {
        var client = AdminClient();

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key = "system.maintenance_mode",
                scope = (int)SettingScope.System,
                value = "yes",
            },
            JsonOptions
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors")
            .TryGetProperty("system.maintenance_mode", out var errs)
            .Should()
            .BeTrue();
        errs.EnumerateArray().Should().Contain(e => e.GetString()!.Contains("boolean"));
    }

    [Fact]
    public async Task Validation_InvalidEmail_Returns400()
    {
        var client = AdminClient();

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key = "app.support_email",
                scope = (int)SettingScope.Application,
                value = "not-an-email",
            },
            JsonOptions
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors")
            .TryGetProperty("app.support_email", out var errs)
            .Should()
            .BeTrue();
        // Document current behavior: 2 errors (type check + pattern) — see also SettingValidatorTests
        errs.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Validation_InvalidColor_Returns400()
    {
        var client = AdminClient();

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key = "app.primary_color",
                scope = (int)SettingScope.Application,
                value = "red",
            },
            JsonOptions
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validation_SelectInvalidOption_Returns400()
    {
        var client = AdminClient();

        var response = await client.PutAsJsonAsync(
            "/api/settings/me",
            new
            {
                key = "user.preferred_density",
                scope = (int)SettingScope.User,
                value = "giant",
            },
            JsonOptions
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors")
            .TryGetProperty("user.preferred_density", out var errs)
            .Should()
            .BeTrue();
        errs.EnumerateArray().Should().Contain(e => e.GetString()!.Contains("one of"));
    }

    // -----------------------------------------------------------------------
    // Bug: missing ?scope= causes 500
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSetting_MissingScopeQueryParam_Returns400()
    {
        // The in-process test host correctly returns 400 when ?scope= is missing.
        // NOTE: The production dev server (dotnet run) returns 500 for this same request,
        // suggesting the production host's exception-handling pipeline differs from the
        // test host. The 500 in production is still a bug — a missing required parameter
        // should always produce 400, not 500.
        var client = AdminClient();

        var response = await client.GetAsync("/api/settings/app.title");

        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.BadRequest,
                "missing ?scope= must return 400 Bad Request. "
                    + "Note: the production server returns 500 for this — see live server probe."
            );
    }

    [Fact]
    public async Task DeleteSetting_MissingScopeQueryParam_Returns400()
    {
        // Same as GetSetting — test host returns 400, production server returns 500.
        var client = AdminClient();

        var response = await client.DeleteAsync("/api/settings/app.title");

        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.BadRequest,
                "missing ?scope= must return 400 Bad Request. "
                    + "Note: the production server returns 500 for this — see live server probe."
            );
    }

    // -----------------------------------------------------------------------
    // Bug: BulkUpdate with User scope throws 500
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BulkUpdate_UserScope_Returns400()
    {
        // The endpoint rejects User-scope entries with 400 (defense-in-depth at the endpoint
        // level, plus SetManyAsync throws SettingValidationException as a backstop).
        var client = AdminClient();

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

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -----------------------------------------------------------------------
    // Bug: PUT /api/settings with scope=User stores userId=null
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateSetting_WithUserScope_IsRejected()
    {
        // PUT /api/settings rejects User scope at the admin endpoint to prevent ghost rows
        // (UserId=null). Callers must use /api/settings/me for per-user values.
        var client = AdminClient();
        var key = UniqueKey("user-scope-ghost");

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.User,
                value = "ghostly",
            },
            JsonOptions
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -----------------------------------------------------------------------
    // Cache invalidation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AfterPut_NextGet_ReflectsNewValue()
    {
        var client = AdminClient();
        var key = UniqueKey("cache");

        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.Application,
                value = "first",
            },
            JsonOptions
        );
        var first = await (
            await client.GetAsync($"/api/settings/{key}?scope=1")
        ).Content.ReadFromJsonAsync<JsonElement>();
        first.GetProperty("value").GetString().Should().Be("first");

        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.Application,
                value = "second",
            },
            JsonOptions
        );
        var second = await (
            await client.GetAsync($"/api/settings/{key}?scope=1")
        ).Content.ReadFromJsonAsync<JsonElement>();
        second
            .GetProperty("value")
            .GetString()
            .Should()
            .Be("second", "cache must be invalidated after PUT");
    }

    [Fact]
    public async Task AfterDelete_GetResolved_ReturnsDefinitionDefault()
    {
        var client = AdminClient();

        // Set a value then delete it; resolved should fall back to definition default.
        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key = "app.primary_color",
                scope = (int)SettingScope.Application,
                value = "#aabbcc",
            },
            JsonOptions
        );

        await client.DeleteAsync("/api/settings/app.primary_color?scope=1");

        var resolved = await client.GetAsync("/api/settings/app.primary_color/resolved");
        resolved.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resolved.Content.ReadFromJsonAsync<JsonElement>();
        // Definition default is "#3b82f6"
        body.GetProperty("value")
            .GetString()
            .Should()
            .Be(
                "#3b82f6",
                "resolved endpoint must return definition default after delete, not a stale cached value"
            );
    }

    // -----------------------------------------------------------------------
    // Scope semantics
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ScopeIsolation_SystemAndApplicationAreSeparateRows()
    {
        var client = AdminClient();
        var key = UniqueKey("scope-isolation");

        // Set different values at System and Application scope
        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.System,
                value = "sys-value",
            },
            JsonOptions
        );
        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.Application,
                value = "app-value",
            },
            JsonOptions
        );

        var sysBody = await (
            await client.GetAsync($"/api/settings/{key}?scope=0")
        ).Content.ReadFromJsonAsync<JsonElement>();
        var appBody = await (
            await client.GetAsync($"/api/settings/{key}?scope=1")
        ).Content.ReadFromJsonAsync<JsonElement>();

        sysBody.GetProperty("value").GetString().Should().Be("sys-value");
        appBody.GetProperty("value").GetString().Should().Be("app-value");
    }

    [Fact]
    public async Task UserScopeResolution_FallsThroughAppThenDefinitionDefault()
    {
        var client = AdminClient();
        var key = "app.primary_color";

        // Ensure no app-scope override exists
        await client.DeleteAsync($"/api/settings/{key}?scope=1");

        // Set an app-scope value
        await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key,
                scope = (int)SettingScope.Application,
                value = "#ff0000",
            },
            JsonOptions
        );

        // The resolved endpoint uses the authenticated user's ID.
        // With no user-scope override the user gets the app value.
        var resolved = await client.GetAsync($"/api/settings/{key}/resolved");
        var body = await resolved.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("value")
            .GetString()
            .Should()
            .Be(
                "#ff0000",
                "resolved must fall back to application scope when no user override exists"
            );

        // Cleanup
        await client.DeleteAsync($"/api/settings/{key}?scope=1");
    }

    // -----------------------------------------------------------------------
    // Permission gate — any authenticated user can write (no Settings.Update check)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AnonymousPut_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key = "app.title",
                scope = (int)SettingScope.Application,
                value = "hacked",
            },
            JsonOptions
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousDelete_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/settings/app.title?scope=1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutSettingsUpdatePermission_GetsForbidden()
    {
        // Write endpoints are gated on Settings.Update. An authenticated user without that
        // permission cannot modify system-scope settings.
        var client = factory.CreateAuthenticatedClient(); // no Settings.Update claim

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new
            {
                key = "system.maintenance_mode",
                scope = (int)SettingScope.System,
                value = false,
            },
            JsonOptions
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
