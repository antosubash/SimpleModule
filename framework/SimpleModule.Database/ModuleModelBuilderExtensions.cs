using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Entities;

namespace SimpleModule.Database;

public static class ModuleModelBuilderExtensions
{
#pragma warning disable CA1308 // Schema names are conventionally lowercase in PostgreSQL/SQL Server
    public static void ApplyModuleSchema(
        this ModelBuilder modelBuilder,
        string moduleName,
        DatabaseOptions dbOptions
    )
    {
        var hasOwnConnection = dbOptions.ModuleConnections.ContainsKey(moduleName);
        if (hasOwnConnection)
            return;

        var connectionString = dbOptions.DefaultConnection;
        var provider = DatabaseProviderDetector.Detect(connectionString);

        if (provider == DatabaseProvider.Sqlite)
        {
            var prefix = $"{moduleName}_";
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                if (
                    tableName is not null
                    && !tableName.StartsWith(prefix, StringComparison.Ordinal)
                )
                {
                    entity.SetTableName($"{prefix}{tableName}");
                }
            }
        }
        else
        {
            var schema = moduleName.ToLowerInvariant();
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetSchema(schema);
            }
        }

        ApplyEntityConventions(modelBuilder, provider);
    }
#pragma warning restore CA1308

    /// <summary>
    /// Applies multi-tenant query filters to all <see cref="IMultiTenant"/> entities.
    /// Call this from your DbContext's <c>OnModelCreating</c> after <see cref="ApplyModuleSchema"/>.
    /// <para>
    /// The filter expression closes over the <paramref name="tenantContext"/> field reference,
    /// so EF Core evaluates the current tenant ID at query time (parameterized).
    /// </para>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyModuleSchema("Products", dbOptions.Value);
    ///     modelBuilder.ApplyMultiTenantFilters(tenantContext);
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public static void ApplyMultiTenantFilters(
        this ModelBuilder modelBuilder,
        ITenantContext tenantContext
    )
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(IMultiTenant.TenantId));
            var tenantContextExpr = Expression.Constant(tenantContext);
            var currentTenantId = Expression.Property(
                tenantContextExpr,
                nameof(ITenantContext.TenantId)
            );

            // Handle null tenant: when no tenant is set, the filter becomes e.TenantId == null
            // which effectively returns no rows (TenantId is non-nullable string).
            var filter = Expression.Lambda(
                Expression.Equal(
                    tenantIdProperty,
                    Expression.Coalesce(currentTenantId, Expression.Constant(""))
                ),
                parameter
            );

            entityType.SetQueryFilter(DatabaseConstants.MultiTenantQueryFilterKey, filter);
        }
    }

    /// <summary>
    /// Applies EF Core conventions for entity interfaces. Guarded against re-entry
    /// so it runs at most once per model even if <see cref="ApplyModuleSchema"/> is called
    /// multiple times.
    /// </summary>
    private static void ApplyEntityConventions(ModelBuilder modelBuilder, DatabaseProvider provider)
    {
        if (
            modelBuilder.Model.FindAnnotation(DatabaseConstants.EntityConventionsAppliedAnnotation)
            is not null
        )
            return;

        modelBuilder.Model.AddAnnotation(
            DatabaseConstants.EntityConventionsAppliedAnnotation,
            true
        );

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(ISoftDelete).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(property), parameter);
                entityType.SetQueryFilter(DatabaseConstants.SoftDeleteQueryFilterKey, filter);
            }

            if (typeof(IHasConcurrencyStamp).IsAssignableFrom(clrType))
            {
                var concurrencyProp = entityType.FindProperty(
                    nameof(IHasConcurrencyStamp.ConcurrencyStamp)
                );
                if (concurrencyProp is not null)
                {
                    concurrencyProp.IsConcurrencyToken = true;
                }
            }

            if (typeof(IVersioned).IsAssignableFrom(clrType))
            {
                var versionProp = entityType.FindProperty(nameof(IVersioned.Version));
                if (versionProp is not null)
                {
                    versionProp.IsConcurrencyToken = true;
                }
            }

            if (typeof(IHasExtraProperties).IsAssignableFrom(clrType))
            {
                var prop = entityType.FindProperty(nameof(IHasExtraProperties.ExtraProperties));
                if (prop is not null)
                {
                    var columnType = provider switch
                    {
                        DatabaseProvider.PostgreSql => "jsonb",
                        DatabaseProvider.SqlServer => "nvarchar(max)",
                        _ => "TEXT",
                    };
                    prop.SetColumnType(columnType);
                }
            }
        }
    }
}
