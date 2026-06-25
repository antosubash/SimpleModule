using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

/// <summary>
/// Full editable branding surface returned by the admin GET endpoint and posted back
/// by the admin form. Includes the file ids and custom CSS that <see cref="BrandingDto"/>
/// omits.
/// </summary>
[Dto]
public class BrandingEditModel
{
    public string AppName { get; set; } = BrandingDefaults.AppName;
    public string? LogoFileId { get; set; }

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
    public string? LogoUrl { get; set; }

    public string? FaviconFileId { get; set; }

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
    public string? FaviconUrl { get; set; }
    public string ColorPrimary { get; set; } = BrandingDefaults.ColorPrimary;
    public string ColorPrimaryDark { get; set; } = BrandingDefaults.ColorPrimaryDark;
    public string CustomCss { get; set; } = string.Empty;
    public TopBarConfig TopBar { get; set; } = new();
    public FooterConfig Footer { get; set; } = new();
}
