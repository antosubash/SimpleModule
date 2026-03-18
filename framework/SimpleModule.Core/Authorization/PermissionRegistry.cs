using System.Collections.Generic;

namespace SimpleModule.Core.Authorization;

public sealed class PermissionRegistry
{
    public IReadOnlySet<string> AllPermissions { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ByModule { get; }

    internal PermissionRegistry(
        IReadOnlySet<string> allPermissions,
        IReadOnlyDictionary<string, IReadOnlyList<string>> byModule)
    {
        AllPermissions = allPermissions;
        ByModule = byModule;
    }
}
