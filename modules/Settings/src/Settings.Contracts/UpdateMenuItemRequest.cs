using SimpleModule.Core;

namespace SimpleModule.Settings.Contracts;

[Dto]
public class UpdateMenuItemRequest
{
    public string Label { get; set; } = "";
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings"
    )]
    public string? Url { get; set; }
    public string? PageRoute { get; set; }
    public string Icon { get; set; } = "";
    public string? CssClass { get; set; }
    public bool OpenInNewTab { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsHomePage { get; set; }
}
