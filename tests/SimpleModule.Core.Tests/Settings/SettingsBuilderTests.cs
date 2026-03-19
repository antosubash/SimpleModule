using FluentAssertions;
using SimpleModule.Core.Settings;

namespace SimpleModule.Core.Tests.Settings;

public class SettingsBuilderTests
{
    [Fact]
    public void Add_SingleDefinition_ReturnsInList()
    {
        var builder = new SettingsBuilder();
        var def = new SettingDefinition
        {
            Key = "app.theme",
            DisplayName = "Theme",
            Scope = SettingScope.Application,
            DefaultValue = "\"light\"",
            Type = SettingType.Text,
        };

        builder.Add(def);

        builder.ToList().Should().ContainSingle().Which.Key.Should().Be("app.theme");
    }

    [Fact]
    public void Add_MultipleDefinitions_ReturnsAll()
    {
        var builder = new SettingsBuilder();
        builder.Add(new SettingDefinition { Key = "a", DisplayName = "A" });
        builder.Add(new SettingDefinition { Key = "b", DisplayName = "B" });

        builder.ToList().Should().HaveCount(2);
    }

    [Fact]
    public void Add_ReturnsSelf_ForChaining()
    {
        var builder = new SettingsBuilder();
        var result = builder.Add(new SettingDefinition { Key = "a", DisplayName = "A" });

        result.Should().BeSameAs(builder);
    }
}
