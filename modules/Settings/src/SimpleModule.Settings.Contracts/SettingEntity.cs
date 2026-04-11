using SimpleModule.Core;
using SimpleModule.Core.Entities;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

[NoDtoGeneration]
public class SettingEntity : Entity<SettingId>
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingScope Scope { get; set; }
    public string? UserId { get; set; }
}
