using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

/// <summary>
/// Resolved branding used as the <c>branding</c> Inertia shared prop (React chrome)
/// and by the head contributor (colors/favicon). Custom CSS is intentionally excluded
/// from this DTO so it never ships in the per-page payload.
/// </summary>
[Dto]
public class BrandingDto
{
    public string AppName { get; set; } = BrandingDefaults.AppName;

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
    public string? LogoUrl { get; set; }

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
    public string? FaviconUrl { get; set; }
    public string ColorPrimary { get; set; } = BrandingDefaults.ColorPrimary;
    public string ColorPrimaryDark { get; set; } = BrandingDefaults.ColorPrimaryDark;
    public TopBarConfig TopBar { get; set; } = new();
    public FooterConfig Footer { get; set; } = new();
}
