using SimpleModule.Core.Authorization;

namespace SimpleModule.Map;

public sealed class MapPermissions : IModulePermissions
{
    // Saved maps
    public const string View = "Map.View";
    public const string Create = "Map.Create";
    public const string Update = "Map.Update";
    public const string Delete = "Map.Delete";

    // Layer source catalog (typically admin-only)
    public const string ViewSources = "Map.ViewSources";
    public const string ManageSources = "Map.ManageSources";
}
