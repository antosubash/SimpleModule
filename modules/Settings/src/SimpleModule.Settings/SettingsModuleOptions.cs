using SimpleModule.Core;

namespace SimpleModule.Settings;

/// <summary>
/// Configurable options for the Settings module.
/// </summary>
public class SettingsModuleOptions : IModuleOptions
{
    /// <summary>
    /// How long settings values are cached in memory before being re-read from the database. Default: 60 seconds.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(60);
}
