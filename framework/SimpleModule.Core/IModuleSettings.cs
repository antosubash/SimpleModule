using SimpleModule.Core.Settings;

namespace SimpleModule.Core;

/// <summary>
/// Implement this interface to define runtime-configurable settings.
/// Preferred over overriding <see cref="IModule.ConfigureSettings"/> on the module class.
/// </summary>
public interface IModuleSettings
{
    void ConfigureSettings(ISettingsBuilder settings);
}
