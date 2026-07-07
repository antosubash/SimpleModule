using System.Diagnostics.CodeAnalysis;
using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

[Dto]
public class BrandingLink
{
    public string Label { get; set; } = string.Empty;

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
    public string Url { get; set; } = string.Empty;
}
