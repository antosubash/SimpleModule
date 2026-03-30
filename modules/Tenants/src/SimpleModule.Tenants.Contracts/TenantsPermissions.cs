using SimpleModule.Core.Authorization;

namespace SimpleModule.Tenants.Contracts;

public sealed class TenantsPermissions : IModulePermissions
{
    public const string View = "Tenants.View";
    public const string Create = "Tenants.Create";
    public const string Update = "Tenants.Update";
    public const string Delete = "Tenants.Delete";
    public const string ChangeStatus = "Tenants.ChangeStatus";
    public const string ManageHosts = "Tenants.ManageHosts";
}
