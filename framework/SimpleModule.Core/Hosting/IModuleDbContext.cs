namespace SimpleModule.Core.Hosting;

/// <summary>
/// Marker interface for module-owned DbContexts that participate in the unified HostDbContext.
/// Implementing this interface signals that a module's DbContext contributes entities to the
/// shared database and requires schema isolation.
/// </summary>
/// <remarks>
/// <para>
/// Currently, all module entities are merged into a single HostDbContext by the source generator.
/// This means all modules share one database connection and one migration history. Schema
/// isolation is provider-dependent (PostgreSQL/SQL Server use schemas, SQLite uses table prefixes).
/// </para>
/// <para>
/// Future direction: modules implementing this interface may opt into independent DbContext
/// resolution, enabling per-module databases, independent migrations, and a migration path
/// toward microservices. The generator will use this interface to determine which modules
/// participate in the unified context vs. manage their own.
/// </para>
/// </remarks>
public interface IModuleDbContext
{
    /// <summary>
    /// The schema name (or table prefix for SQLite) used to isolate this module's entities.
    /// </summary>
    static abstract string SchemaName { get; }
}
