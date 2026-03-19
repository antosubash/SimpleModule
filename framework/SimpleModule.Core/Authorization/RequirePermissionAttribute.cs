using System;

namespace SimpleModule.Core.Authorization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string[] Permissions { get; }

    public RequirePermissionAttribute(params string[] permissions)
    {
        Permissions = permissions;
    }
}
