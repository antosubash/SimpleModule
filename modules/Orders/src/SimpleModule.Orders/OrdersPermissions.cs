using SimpleModule.Core.Authorization;

namespace SimpleModule.Orders;

public sealed class OrdersPermissions : IModulePermissions
{
    public const string View = "Orders.View";
    public const string Create = "Orders.Create";
    public const string Update = "Orders.Update";
    public const string Delete = "Orders.Delete";
}
