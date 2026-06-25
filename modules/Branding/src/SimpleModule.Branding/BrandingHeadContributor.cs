using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Branding;

/// <summary>
/// Injects branding CSS variable overrides, custom CSS, and a favicon link into the
/// document <c>&lt;head&gt;</c> server-side so colors apply before first paint (no flash).
/// Inline styles are permitted by the app's CSP (<c>style-src 'unsafe-inline'</c>).
/// </summary>
public sealed partial class BrandingHeadContributor(IBrandingContracts branding)
    : IInertiaHeadContributor
{
    public async ValueTask<string?> GetHeadHtmlAsync(HttpContext context)
    {
        var b = await branding.GetBrandingAsync();
        var css = await branding.GetCustomCssAsync();

        var colorChanged =
            !ColorEquals(b.ColorPrimary, BrandingDefaults.ColorPrimary)
            || !ColorEquals(b.ColorPrimaryDark, BrandingDefaults.ColorPrimaryDark);
        var hasCss = !string.IsNullOrWhiteSpace(css);

        var sb = new StringBuilder();
        if (colorChanged || hasCss)
        {
            sb.Append("<style>");
            if (colorChanged)
            {
                sb.Append(":root{--color-primary:")
                    .Append(SanitizeColor(b.ColorPrimary))
                    .Append(";}");
                sb.Append(".dark{--color-primary:")
                    .Append(SanitizeColor(b.ColorPrimaryDark))
                    .Append(";}");
            }
            if (hasCss)
                sb.Append(StripClosingStyle(css));
            sb.Append("</style>");
        }

        if (!string.IsNullOrWhiteSpace(b.FaviconUrl))
            sb.Append("<link rel=\"icon\" href=\"")
                .Append(EncodeAttr(b.FaviconUrl))
                .Append("\" />");

        return sb.Length == 0 ? null : sb.ToString();
    }

    private static bool ColorEquals(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string SanitizeColor(string color) =>
        HexColor().IsMatch(color) ? color : BrandingDefaults.ColorPrimary;

    private static string StripClosingStyle(string css) =>
        ClosingStyle().Replace(css, string.Empty);

    private static string EncodeAttr(string value) =>
        value.Replace("\"", "&quot;", StringComparison.Ordinal);

    [GeneratedRegex("^#[0-9a-fA-F]{3,8}$")]
    private static partial Regex HexColor();

    [GeneratedRegex("</style>", RegexOptions.IgnoreCase)]
    private static partial Regex ClosingStyle();
}
