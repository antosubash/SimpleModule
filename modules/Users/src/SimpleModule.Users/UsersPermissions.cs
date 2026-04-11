using SimpleModule.Core.Authorization;

namespace SimpleModule.Users;

public sealed class UsersPermissions : IModulePermissions
{
    public const string View = "Users.View";
    public const string Create = "Users.Create";
    public const string Update = "Users.Update";
    public const string Delete = "Users.Delete";
    public const string ManageRoles = "Users.ManageRoles";
}
