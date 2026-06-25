using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

[Dto]
public class TopBarConfig
{
    public bool Enabled { get; set; }
    public string Message { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#059669";
    public string TextColor { get; set; } = "#ffffff";
    public List<BrandingLink> Links { get; set; } = [];
    public bool Dismissible { get; set; } = true;
}
