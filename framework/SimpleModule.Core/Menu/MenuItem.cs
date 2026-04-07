using System.Collections.Generic;

namespace SimpleModule.Core.Menu;

public sealed class MenuItem
{
    public required string Label { get; init; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings"
    )]
    public required string Url { get; init; }
    public string Icon { get; init; } = "";
    public int Order { get; init; }
    public MenuSection Section { get; init; } = MenuSection.Navbar;
    public bool RequiresAuth { get; init; } = true;
    public string? Group { get; init; }

    /// <summary>
    /// When set, this menu item is only visible to users who have at least one of these roles.
    /// An empty list means visible to all authenticated users.
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = [];
}
