using SimpleModule.Core.Entities;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Entities;

public class PublicMenuItemEntity : Entity<PublicMenuItemId>
{
    public PublicMenuItemId? ParentId { get; set; }
    public PublicMenuItemEntity? Parent { get; set; }
    public List<PublicMenuItemEntity> Children { get; set; } = [];

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
    public int SortOrder { get; set; }
}
