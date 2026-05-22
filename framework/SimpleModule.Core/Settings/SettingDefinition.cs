using System.Collections.Generic;
using SimpleModule.Core;

namespace SimpleModule.Core.Settings;

[Dto]
public class SettingDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public SettingScope Scope { get; set; }
    public string? DefaultValue { get; set; }
    public SettingType Type { get; set; }
    public IReadOnlyList<string>? AllowedValues { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string? Pattern { get; set; }
    public bool Required { get; set; }
    public bool Sensitive { get; set; }
    public int Order { get; set; }
    public string? Placeholder { get; set; }
}
