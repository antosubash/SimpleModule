using FluentAssertions;
using SimpleModule.Core.Settings;

namespace SimpleModule.Core.Tests.Settings;

public class SettingsDefinitionRegistryTests
{
    [Fact]
    public void GetDefinitions_NoFilter_ReturnsAll()
    {
        var registry = CreateRegistry();
        registry.GetDefinitions().Should().HaveCount(3);
    }

    [Fact]
    public void GetDefinitions_FilterByScope_ReturnsMatching()
    {
        var registry = CreateRegistry();
        registry.GetDefinitions(SettingScope.System).Should().ContainSingle();
    }

    [Fact]
    public void GetDefinitions_NoMatchingScope_ReturnsEmpty()
    {
        var registry = new SettingsDefinitionRegistry([]);
        registry.GetDefinitions(SettingScope.System).Should().BeEmpty();
    }

    [Fact]
    public void GetDefinition_ExistingKey_ReturnsDefinition()
    {
        var registry = CreateRegistry();
        var def = registry.GetDefinition("smtp.host");
        def.Should().NotBeNull();
        def!.DisplayName.Should().Be("SMTP Host");
    }

    [Fact]
    public void GetDefinition_UnknownKey_ReturnsNull()
    {
        var registry = CreateRegistry();
        registry.GetDefinition("nonexistent").Should().BeNull();
    }

    private static SettingsDefinitionRegistry CreateRegistry() =>
        new(
        [
            new SettingDefinition
            {
                Key = "smtp.host",
                DisplayName = "SMTP Host",
                Group = "Email",
                Scope = SettingScope.System,
                Type = SettingType.Text,
            },
            new SettingDefinition
            {
                Key = "app.title",
                DisplayName = "App Title",
                Group = "General",
                Scope = SettingScope.Application,
                DefaultValue = "\"SimpleModule\"",
                Type = SettingType.Text,
            },
            new SettingDefinition
            {
                Key = "user.theme",
                DisplayName = "Theme",
                Group = "Appearance",
                Scope = SettingScope.User,
                DefaultValue = "\"light\"",
                Type = SettingType.Text,
            },
        ]);
}
