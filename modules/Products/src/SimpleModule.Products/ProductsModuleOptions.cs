using SimpleModule.Core;

namespace SimpleModule.Products;

/// <summary>
/// Configurable options for the Products module.
/// Override defaults from the host application:
/// <code>
/// builder.AddSimpleModule(o =&gt;
/// {
///     o.ConfigureProducts(p =&gt; p.DefaultPageSize = 20);
/// });
/// </code>
/// </summary>
public class ProductsModuleOptions : IModuleOptions
{
    /// <summary>
    /// Default number of products per page when browsing. Default: 10.
    /// </summary>
    public int DefaultPageSize { get; set; } = 10;

    /// <summary>
    /// Maximum allowed page size for product listing requests. Default: 100.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;
}
