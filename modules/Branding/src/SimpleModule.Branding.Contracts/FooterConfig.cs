using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

[Dto]
public class FooterConfig
{
    public bool Enabled { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<BrandingLink> Links { get; set; } = [];
    public bool ShowCopyright { get; set; } = true;
}
