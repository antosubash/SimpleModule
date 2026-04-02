using SimpleModule.Core.Menu;

namespace SimpleModule.Core;

/// <summary>
/// Implement this interface to contribute navigation menu items.
/// Preferred over overriding <see cref="IModule.ConfigureMenu"/> on the module class.
/// </summary>
public interface IModuleMenu
{
    void ConfigureMenu(IMenuBuilder menus);
}
