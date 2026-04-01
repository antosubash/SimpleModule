namespace SimpleModule.Database;

public static class DatabaseConstants
{
    public const string SectionName = "Database";
    public const string HostModuleName = "Host";
    public const string PostgresHostPrefix = "Host=";
    public const string SqlServerCatalogPrefix = "Initial Catalog=";
    public const string SqlServerLocalPrefix = @"Server=.\";
    public const string SqlServerExpressionPrefix = @"Server=(";

    /// <summary>Named query filter key for <see cref="Core.Entities.ISoftDelete"/> entities.</summary>
    public const string SoftDeleteQueryFilterKey = "SimpleModule:SoftDelete";

    /// <summary>Named query filter key for <see cref="Core.Entities.IMultiTenant"/> entities.</summary>
    public const string MultiTenantQueryFilterKey = "SimpleModule:MultiTenant";

    internal const string EntityConventionsAppliedAnnotation =
        "SimpleModule:EntityConventionsApplied";
}
