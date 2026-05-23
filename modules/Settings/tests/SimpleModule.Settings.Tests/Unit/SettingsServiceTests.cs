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
using SimpleModule.Tests.Shared.Fakes;
using Wolverine;
using ZiggyCreatures.Caching.Fusion;

namespace Settings.Tests.Unit;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly SettingsDbContext _db;
    private readonly FusionCache _cache;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:" }
        );
        _db = new SettingsDbContext(options, dbOptions);
        _db.Database.EnsureCreated();

        var registry = new SettingsDefinitionRegistry([
            new SettingDefinition
            {
                Key = "theme",
                DisplayName = "Theme",
                Scope = SettingScope.User,
                DefaultValue = "\"light\"",
                Type = SettingType.Text,
            },
        ]);

        _cache = new FusionCache(new FusionCacheOptions());

        _service = new SettingsService(
            _db,
            registry,
            _cache,
            new Lazy<IMessageBus>(() => Substitute.For<IMessageBus>()),
            Options.Create(new SettingsModuleOptions()),
            NullLogger<SettingsService>.Instance
        );
    }

    private static JsonElement JsonString(string s) =>
        JsonSerializer.Deserialize<JsonElement>($"\"{s}\"");

    private static JsonElement JsonNumber(double n) =>
        JsonSerializer.Deserialize<JsonElement>(
            n.ToString(System.Globalization.CultureInfo.InvariantCulture)
        );

    [Fact]
    public async Task ResolveUserSettingAsync_ReturnsUserValue_WhenSet()
    {
        await _service.SetSettingAsync("theme", JsonString("dark"), SettingScope.User, "user1");
        await _service.SetSettingAsync(
            "theme",
            JsonString("system-default"),
            SettingScope.Application
        );

        var result = await _service.ResolveUserSettingAsync("theme", "user1");

        result.Should().Be("\"dark\"");
    }

    [Fact]
    public async Task ResolveUserSettingAsync_FallsBackToApp_WhenNoUserValue()
    {
        await _service.SetSettingAsync("theme", JsonString("corporate"), SettingScope.Application);

        var result = await _service.ResolveUserSettingAsync("theme", "user1");

        result.Should().Be("\"corporate\"");
    }

    [Fact]
    public async Task ResolveUserSettingAsync_FallsBackToCodeDefault_WhenNothingSet()
    {
        var result = await _service.ResolveUserSettingAsync("theme", "user1");

        result.Should().Be("\"light\"");
    }

    [Fact]
    public async Task SetSettingAsync_Upserts_WhenKeyAlreadyExists()
    {
        await _service.SetSettingAsync("theme", JsonString("dark"), SettingScope.Application);
        await _service.SetSettingAsync("theme", JsonString("blue"), SettingScope.Application);

        var value = await _service.GetSettingAsync("theme", SettingScope.Application);
        value.Should().Be("\"blue\"");

        var count = await _db.Settings.CountAsync(s =>
            s.Key == "theme" && s.Scope == SettingScope.Application
        );
        count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSettingAsync_RemovesSetting()
    {
        await _service.SetSettingAsync("theme", JsonString("dark"), SettingScope.User, "user1");
        await _service.DeleteSettingAsync("theme", SettingScope.User, "user1");

        var value = await _service.GetSettingAsync("theme", SettingScope.User, "user1");
        value.Should().BeNull();
    }

    [Fact]
    public async Task GetSettingAsync_Generic_DeserializesCorrectly()
    {
        await _service.SetSettingAsync("count", JsonNumber(42), SettingScope.Application);

        var result = await _service.GetSettingAsync<int>("count", SettingScope.Application);
        result.Should().Be(42);
    }

    [Fact]
    public async Task GetSettingAsync_NoDbValue_ReturnsNull()
    {
        var result = await _service.GetSettingAsync("nonexistent.key", SettingScope.System);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSettingAsync_Bool_NoDbValue_ReturnsFalse()
    {
        var result = await _service.GetSettingAsync<bool>("nonexistent.key", SettingScope.System);

        result
            .Should()
            .BeFalse(
                "GetSettingAsync<bool> returns default(bool) = false for missing settings; "
                    + "callers must use the string overload to distinguish 'not set' from 'disabled'"
            );
    }

    [Fact]
    public async Task GetSettingValueAsync_ReturnsDto_WithDecodedValue()
    {
        await _service.SetSettingAsync("theme", JsonString("dark"), SettingScope.User, "user1");

        var dto = await _service.GetSettingValueAsync("theme", SettingScope.User, "user1");

        dto.Should().NotBeNull();
        dto!.Key.Should().Be("theme");
        dto.IsOverridden.Should().BeTrue();
        dto.Value.Should().NotBeNull();
        dto.Value!.Value.GetString().Should().Be("dark");
    }

    [Fact]
    public async Task GetSettingValueAsync_ReturnsNull_WhenNotSet()
    {
        var dto = await _service.GetSettingValueAsync("nonexistent.key", SettingScope.Application);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task ResetToDefaultAsync_RemovesSetting()
    {
        await _service.SetSettingAsync("theme", JsonString("dark"), SettingScope.User, "user1");
        await _service.ResetToDefaultAsync("theme", SettingScope.User, "user1");

        var value = await _service.GetSettingAsync("theme", SettingScope.User, "user1");
        value.Should().BeNull();
    }

    [Fact]
    public async Task SetManyAsync_UpsertsBulk()
    {
        var updates = new List<BulkSettingUpdate>
        {
            new()
            {
                Key = "app.a",
                Scope = SettingScope.Application,
                Value = JsonString("val1"),
            },
            new()
            {
                Key = "app.b",
                Scope = SettingScope.Application,
                Value = JsonString("val2"),
            },
        };

        await _service.SetManyAsync(updates);

        var a = await _service.GetSettingAsync("app.a", SettingScope.Application);
        var b = await _service.GetSettingAsync("app.b", SettingScope.Application);
        a.Should().Be("\"val1\"");
        b.Should().Be("\"val2\"");
    }

    // BUG-5: SetManyAsync with User scope should throw SettingValidationException (→ 400),
    // not InvalidOperationException (→ 500).
    [Fact]
    public async Task SetManyAsync_ThrowsSettingValidationException_ForUserScope()
    {
        var updates = new List<BulkSettingUpdate>
        {
            new()
            {
                Key = "theme",
                Scope = SettingScope.User,
                Value = JsonString("dark"),
            },
        };

        var act = () => _service.SetManyAsync(updates);
        await act.Should()
            .ThrowAsync<SettingValidationException>(
                "bulk updates must reject User scope with a clean 400 error, not a 500"
            );
    }

    [Fact]
    public async Task Validate_Number_RejectsString()
    {
        var registry = new SettingsDefinitionRegistry([
            new SettingDefinition
            {
                Key = "count",
                DisplayName = "Count",
                Scope = SettingScope.Application,
                Type = SettingType.Number,
            },
        ]);
        var svc = new SettingsService(
            _db,
            registry,
            _cache,
            new Lazy<IMessageBus>(() => Substitute.For<IMessageBus>()),
            Options.Create(new SettingsModuleOptions()),
            NullLogger<SettingsService>.Instance
        );

        var act = () =>
            svc.SetSettingAsync("count", JsonString("not-a-number"), SettingScope.Application);
        await act.Should().ThrowAsync<SettingValidationException>();
    }

    // BUG-1: Email type with a Pattern should produce exactly ONE validation error,
    // not two (one from ValidateType + one from ValidatePattern).
    [Fact]
    public async Task Validate_Email_WithPattern_ProducesExactlyOneError()
    {
        var registry = new SettingsDefinitionRegistry([
            new SettingDefinition
            {
                Key = "contact.email",
                DisplayName = "Contact Email",
                Scope = SettingScope.Application,
                Type = SettingType.Email,
                // Pattern would also reject the value, but should be skipped for Email type.
                Pattern = @"^[^@]+@example\.com$",
            },
        ]);
        var svc = new SettingsService(
            _db,
            registry,
            _cache,
            new Lazy<IMessageBus>(() => Substitute.For<IMessageBus>()),
            Options.Create(new SettingsModuleOptions()),
            NullLogger<SettingsService>.Instance
        );

        var ex = await Assert.ThrowsAsync<SettingValidationException>(() =>
            svc.SetSettingAsync(
                "contact.email",
                JsonString("not-an-email"),
                SettingScope.Application
            )
        );
        ex.Errors
            .Should()
            .HaveCount(1, "ValidatePattern must be skipped for Email type to avoid duplicate errors");
    }

    // BUG-2: URL validator must reject file:// URIs, relative paths, and non-http/https schemes.
    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("/foo/bar")]
    [InlineData("ftp://example.com/file")]
    public async Task Validate_Url_RejectsNonHttpSchemes(string value)
    {
        var registry = new SettingsDefinitionRegistry([
            new SettingDefinition
            {
                Key = "site.url",
                DisplayName = "Site URL",
                Scope = SettingScope.Application,
                Type = SettingType.Url,
            },
        ]);
        var svc = new SettingsService(
            _db,
            registry,
            _cache,
            new Lazy<IMessageBus>(() => Substitute.For<IMessageBus>()),
            Options.Create(new SettingsModuleOptions()),
            NullLogger<SettingsService>.Instance
        );

        var act = () => svc.SetSettingAsync("site.url", JsonString(value), SettingScope.Application);
        await act.Should().ThrowAsync<SettingValidationException>();
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:5001/api")]
    public async Task Validate_Url_AcceptsHttpAndHttps(string value)
    {
        var registry = new SettingsDefinitionRegistry([
            new SettingDefinition
            {
                Key = "site.url2",
                DisplayName = "Site URL",
                Scope = SettingScope.Application,
                Type = SettingType.Url,
            },
        ]);
        var svc = new SettingsService(
            _db,
            registry,
            _cache,
            new Lazy<IMessageBus>(() => Substitute.For<IMessageBus>()),
            Options.Create(new SettingsModuleOptions()),
            NullLogger<SettingsService>.Instance
        );

        var act = () =>
            svc.SetSettingAsync("site.url2", JsonString(value), SettingScope.Application);
        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        _cache.Dispose();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
