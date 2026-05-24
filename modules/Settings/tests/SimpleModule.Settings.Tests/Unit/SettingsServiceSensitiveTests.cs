using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings;
using SimpleModule.Settings.Contracts;
using Wolverine;
using ZiggyCreatures.Caching.Fusion;

namespace Settings.Tests.Unit;

/// <summary>
/// Tests for Sensitive masking in SettingsService.
/// No seeded Sensitive=true definition exists in production, so we build
/// an in-process registry with Sensitive=true here.
/// </summary>
public sealed class SettingsServiceSensitiveTests : IDisposable
{
    private readonly SettingsDbContext _db;
    private readonly FusionCache _cache;
    private readonly ISettingsDefinitionRegistry _registry;
    private readonly SettingsService _service;

    public SettingsServiceSensitiveTests()
    {
        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:" }
        );
        _db = new SettingsDbContext(options, dbOptions);
        _db.Database.EnsureCreated();

        _registry = new SettingsDefinitionRegistry([
            new SettingDefinition
            {
                Key = "app.api_secret",
                DisplayName = "API Secret",
                Scope = SettingScope.Application,
                Type = SettingType.Password,
                Sensitive = true,
            },
            new SettingDefinition
            {
                Key = "app.public_name",
                DisplayName = "App Name",
                Scope = SettingScope.Application,
                Type = SettingType.Text,
                Sensitive = false,
            },
        ]);

        _cache = new FusionCache(new FusionCacheOptions());
        _service = new SettingsService(
            _db,
            _registry,
            _cache,
            new Lazy<IMessageBus>(() => Substitute.For<IMessageBus>()),
            Options.Create(new SettingsModuleOptions()),
            NullLogger<SettingsService>.Instance
        );
    }

    private static JsonElement JsonString(string s) =>
        JsonSerializer.Deserialize<JsonElement>($"\"{s}\"");

    [Fact]
    public async Task GetSettingValuesAsync_MasksSensitiveValue_ReturnsNullValue()
    {
        await _service.SetSettingAsync(
            "app.api_secret",
            JsonString("super-secret-token"),
            SettingScope.Application
        );

        var results = await _service.GetSettingValuesAsync();

        var secret = results.Single(r => r.Key == "app.api_secret");
        secret.Value.Should().BeNull("sensitive values must be masked in list responses");
    }

    [Fact]
    public async Task GetSettingValueAsync_MasksSensitiveValue_ReturnsNullValue()
    {
        await _service.SetSettingAsync(
            "app.api_secret",
            JsonString("super-secret-token"),
            SettingScope.Application
        );

        var dto = await _service.GetSettingValueAsync("app.api_secret", SettingScope.Application);

        dto.Should().NotBeNull();
        dto!.Value.Should().BeNull("sensitive values must be masked in single-key responses");
    }

    [Fact]
    public async Task GetSettingAsync_SensitiveSetting_StillReturnsRawValueForInternalCallers()
    {
        // GetSettingAsync is the internal service call (used by resolvers, not the API DTOs).
        // It returns the raw stored string. The masking only applies at the DTO layer.
        await _service.SetSettingAsync(
            "app.api_secret",
            JsonString("super-secret-token"),
            SettingScope.Application
        );

        var raw = await _service.GetSettingAsync("app.api_secret", SettingScope.Application);

        raw.Should().NotBeNull("internal callers need the actual value");
        raw.Should().Be("\"super-secret-token\"");
    }

    [Fact]
    public async Task GetSettingValuesAsync_NonSensitiveSetting_ReturnsDecodedValue()
    {
        await _service.SetSettingAsync(
            "app.public_name",
            JsonString("My Application"),
            SettingScope.Application
        );

        var results = await _service.GetSettingValuesAsync();

        var name = results.Single(r => r.Key == "app.public_name");
        name.Value.Should().NotBeNull("non-sensitive values must NOT be masked");
        name.Value!.Value.GetString().Should().Be("My Application");
    }

    [Fact]
    public async Task GetSettingValuesAsync_SensitiveNotPoisonsCacheForNonSensitive()
    {
        // Store both settings and call GetSettingValuesAsync twice.
        // The sensitive masking must not write null into the cache for the sensitive key
        // in a way that GetSettingAsync would subsequently return null.
        await _service.SetSettingAsync(
            "app.api_secret",
            JsonString("tok-abc"),
            SettingScope.Application
        );
        await _service.SetSettingAsync(
            "app.public_name",
            JsonString("Visible Name"),
            SettingScope.Application
        );

        _ = await _service.GetSettingValuesAsync();
        _ = await _service.GetSettingValuesAsync();

        // Internal lookup must still return the actual stored value
        var raw = await _service.GetSettingAsync("app.api_secret", SettingScope.Application);
        raw.Should().Be("\"tok-abc\"", "GetSettingValuesAsync masking must not poison the cache");

        var publicRaw = await _service.GetSettingAsync("app.public_name", SettingScope.Application);
        publicRaw.Should().Be("\"Visible Name\"");
    }

    public void Dispose()
    {
        _cache.Dispose();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
