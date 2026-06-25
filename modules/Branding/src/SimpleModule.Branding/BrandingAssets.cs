using SimpleModule.Branding.Contracts;

namespace SimpleModule.Branding;

/// <summary>
/// Single source of truth for branding asset handling shared by the upload endpoint,
/// the anonymous serve endpoint, and <see cref="BrandingService"/>.
/// </summary>
internal static class BrandingAssets
{
    /// <summary>
    /// Raster + icon formats only. SVG is excluded on BOTH the upload and serve paths:
    /// assets are served anonymously and an SVG can carry inline script that executes on
    /// direct navigation (stored XSS). Raster formats cannot.
    /// </summary>
    public static readonly HashSet<string> AllowedContentTypes = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "image/x-icon",
        "image/vnd.microsoft.icon",
    };

    public static bool IsValidKind(string kind) => kind is "logo" or "favicon";

    public static string SettingKey(string kind) =>
        kind == "logo" ? BrandingSettingKeys.LogoFileId : BrandingSettingKeys.FaviconFileId;

    public static string Url(string kind, string fileId) =>
        $"/api/branding/assets/{kind}?v={fileId}";
}
