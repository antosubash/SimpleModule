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
}
