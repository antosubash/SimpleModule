namespace SimpleModule.Database;

public sealed class DatabaseOptions
{
    public string DefaultConnection { get; set; } = string.Empty;
    public Dictionary<string, string> ModuleConnections { get; set; } = [];

    /// <summary>
    /// Explicit database provider. When set, overrides connection-string-based auto-detection.
    /// Valid values: "Sqlite", "PostgreSql", "SqlServer".
    /// </summary>
    public string? Provider { get; set; }
}
