using SimpleModule.Core;

namespace SimpleModule.Orders;

/// <summary>
/// Configurable options for the Orders module.
/// </summary>
public class OrdersModuleOptions : IModuleOptions
{
    /// <summary>
    /// Default number of recent orders to show in summary views. Default: 10.
    /// </summary>
    public int DefaultPageSize { get; set; } = 10;

    /// <summary>
    /// Maximum allowed page size for order listing requests. Default: 100.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;
}
