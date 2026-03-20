namespace SimpleModule.Core.Menu;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings"
)]
public sealed record AvailablePage(string PageRoute, string ViewPrefix, string Module);
