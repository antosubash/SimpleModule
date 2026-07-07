using System.Text;
using System.Text.Encodings.Web;
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
                // Derive the full primary palette (hover/light/subtle/ring) from the
                // chosen color via color-mix so button hovers and focus rings follow the
                // brand color instead of staying on the theme default. The app already
                // uses color-mix widely, so support matches the rest of the UI.
                AppendPrimaryPalette(sb, ":root", SanitizeColor(b.ColorPrimary), dark: false);
                AppendPrimaryPalette(sb, ".dark", SanitizeColor(b.ColorPrimaryDark), dark: true);
            }
            if (hasCss)
                sb.Append(StripStyleEndTag(css));
            sb.Append("</style>");
        }

        if (!string.IsNullOrWhiteSpace(b.FaviconUrl))
            sb.Append("<link rel=\"icon\" href=\"")
                .Append(HtmlEncoder.Default.Encode(b.FaviconUrl))
                .Append("\" />");

        return sb.Length == 0 ? null : sb.ToString();
    }

    private static void AppendPrimaryPalette(StringBuilder sb, string selector, string c, bool dark)
    {
        sb.Append(selector).Append('{');
        sb.Append("--color-primary:").Append(c).Append(';');
        // Light theme darkens on hover; dark theme lightens (mirrors the base theme).
        sb.Append("--color-primary-hover:color-mix(in oklab,")
            .Append(c)
            .Append(dark ? ",white 10%);" : ",black 12%);");
        sb.Append("--color-primary-light:color-mix(in oklab,")
            .Append(c)
            .Append(dark ? ",white 25%);" : ",white 22%);");
        sb.Append("--color-primary-subtle:color-mix(in oklab,")
            .Append(c)
            .Append(dark ? " 14%,transparent);" : " 9%,transparent);");
        sb.Append("--color-primary-ring:color-mix(in oklab,")
            .Append(c)
            .Append(dark ? " 40%,transparent);" : " 32%,transparent);");
        sb.Append('}');
    }

    private static bool ColorEquals(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string SanitizeColor(string color) =>
        HexColor().IsMatch(color) ? color : BrandingDefaults.ColorPrimary;

    // Strip any "</style" sequence (not just the exact "</style>"): the HTML raw-text
    // parser ends a <style> element on "</style" followed by whitespace, "/", or ">",
    // so admin custom CSS like "}</style ><img ...>" must not be able to break out.
    private static string StripStyleEndTag(string css) => StyleEndTag().Replace(css, string.Empty);

    [GeneratedRegex("^#[0-9a-fA-F]{3,8}$")]
    private static partial Regex HexColor();

    [GeneratedRegex("</style", RegexOptions.IgnoreCase)]
    private static partial Regex StyleEndTag();
}
