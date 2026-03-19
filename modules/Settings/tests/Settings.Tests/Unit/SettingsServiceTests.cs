using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings;

namespace Settings.Tests.Unit;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly SettingsDbContext _db;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbOptions = Options.Create(new DatabaseOptions());
        _db = new SettingsDbContext(options, dbOptions);
        _db.Database.EnsureCreated();

        var registry = new SettingsDefinitionRegistry(
            [
                new SettingDefinition
                {
                    Key = "theme",
                    DisplayName = "Theme",
                    Scope = SettingScope.User,
                    DefaultValue = "\"light\"",
                    Type = SettingType.Text,
                },
            ]
        );

        _service = new SettingsService(_db, registry, NullLogger<SettingsService>.Instance);
    }

    [Fact]
    public async Task ResolveUserSettingAsync_ReturnsUserValue_WhenSet()
    {
        await _service.SetSettingAsync("theme", "\"dark\"", SettingScope.User, "user1");
        await _service.SetSettingAsync("theme", "\"system-default\"", SettingScope.Application);

        var result = await _service.ResolveUserSettingAsync("theme", "user1");

        result.Should().Be("\"dark\"");
    }

    [Fact]
    public async Task ResolveUserSettingAsync_FallsBackToApp_WhenNoUserValue()
    {
        await _service.SetSettingAsync("theme", "\"corporate\"", SettingScope.Application);

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
        await _service.SetSettingAsync("theme", "\"dark\"", SettingScope.Application);
        await _service.SetSettingAsync("theme", "\"blue\"", SettingScope.Application);

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
        await _service.SetSettingAsync("theme", "\"dark\"", SettingScope.User, "user1");
        await _service.DeleteSettingAsync("theme", SettingScope.User, "user1");

        var value = await _service.GetSettingAsync("theme", SettingScope.User, "user1");
        value.Should().BeNull();
    }

    [Fact]
    public async Task GetSettingAsync_Generic_DeserializesCorrectly()
    {
        await _service.SetSettingAsync("count", "42", SettingScope.Application);

        var result = await _service.GetSettingAsync<int>("count", SettingScope.Application);
        result.Should().Be(42);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
