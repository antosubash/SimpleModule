using SimpleModule.Core;

namespace SimpleModule.PageBuilder;

/// <summary>
/// Configurable options for the PageBuilder module.
/// </summary>
public class PageBuilderModuleOptions : IModuleOptions
{
    /// <summary>
    /// Maximum length for page titles. Default: 200.
    /// </summary>
    public int MaxTitleLength { get; set; } = 200;

    /// <summary>
    /// Maximum length for page slugs. Default: 200.
    /// </summary>
    public int MaxSlugLength { get; set; } = 200;
}
