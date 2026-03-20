namespace SimpleModule.Core.Menu;

public sealed class PublicMenuItem
{
    public required string Label { get; init; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings"
    )]
    public string? Url { get; init; }

    public int Order { get; init; }
    public string? CssClass { get; init; }
    public bool OpenInNewTab { get; init; }
    public IReadOnlyList<PublicMenuItem> Children { get; init; } = [];
}
