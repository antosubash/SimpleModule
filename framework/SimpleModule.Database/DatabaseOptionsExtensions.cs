namespace SimpleModule.Database;

/// <summary>
/// Convenience helpers for resolving a <see cref="DatabaseProvider"/> from a
/// <see cref="DatabaseOptions"/> instance, tolerating missing defaults by
/// falling back to module-specific connections or explicit provider names.
/// </summary>
public static class DatabaseOptionsExtensions
{
    /// <summary>
    /// Detects the database provider for <paramref name="moduleName"/>, honoring
    /// module-specific connection strings and explicit <see cref="DatabaseOptions.Provider"/>.
    /// Returns <c>null</c> when neither a connection string nor an explicit provider is configured.
    /// </summary>
    public static DatabaseProvider? DetectProvider(this DatabaseOptions options, string moduleName)
    {
        ArgumentNullException.ThrowIfNull(options);

        var cs = options.ModuleConnections.TryGetValue(moduleName, out var module)
            ? module
            : options.DefaultConnection;

        if (string.IsNullOrWhiteSpace(cs) && string.IsNullOrWhiteSpace(options.Provider))
        {
            return null;
        }

        return DatabaseProviderDetector.Detect(cs ?? string.Empty, options.Provider);
    }
}
