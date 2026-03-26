using SimpleModule.Core.Authorization;

namespace SimpleModule.Admin.Contracts;

public sealed class AdminPermissions : IModulePermissions
{
    public const string ManageUsers = "Admin.ManageUsers";
    public const string ManageRoles = "Admin.ManageRoles";
    public const string ViewAuditLog = "Admin.ViewAuditLog";
}
