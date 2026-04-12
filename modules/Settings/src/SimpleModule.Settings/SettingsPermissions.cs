using SimpleModule.Core.Authorization;

namespace SimpleModule.Settings;

public sealed class SettingsPermissions : IModulePermissions
{
    public const string View = "Settings.View";
    public const string Update = "Settings.Update";
    public const string ManageMenus = "Settings.ManageMenus";
}
