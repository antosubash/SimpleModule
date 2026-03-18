using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleModule.Core.Authorization;

public sealed class PermissionRegistryBuilder
{
    private readonly Dictionary<string, List<string>> _byModule = new();

    public void AddPermissions<T>() where T : class
    {
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            var value = (string)field.GetRawConstantValue()!;
            AddPermission(value);
        }
    }

    public void AddPermission(string permission)
    {
        var dotIndex = permission.IndexOf('.', StringComparison.Ordinal);
        var module = dotIndex >= 0 ? permission[..dotIndex] : "Global";

        if (!_byModule.TryGetValue(module, out var list))
        {
            list = [];
            _byModule[module] = list;
        }

        if (!list.Contains(permission))
        {
            list.Add(permission);
        }
    }

    public PermissionRegistry Build()
    {
        var all = new HashSet<string>();
        var byModule = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var (module, permissions) in _byModule)
        {
            byModule[module] = permissions.AsReadOnly();
            foreach (var p in permissions)
            {
                all.Add(p);
            }
        }

        return new PermissionRegistry(all, byModule);
    }
}
