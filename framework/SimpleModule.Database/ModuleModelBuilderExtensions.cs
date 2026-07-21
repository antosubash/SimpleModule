using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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

        // Honor an explicitly-configured provider; only fall back to sniffing the
        // connection string when none is set. Otherwise a host that sets
        // Database:Provider but leaves a connection string that looks like another
        // provider would pick the wrong schema strategy (#227).
        var connectionString = dbOptions.DefaultConnection;
        var provider = DatabaseProviderDetector.Detect(connectionString, dbOptions.Provider);

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

    /// <summary>
    /// Applies schema isolation for the unified host <see cref="ModelBuilder"/> that merges
    /// entities from every module. Unlike <see cref="ApplyModuleSchema"/> — which owns a single
    /// module's context and can safely prefix <em>all</em> entities — the host model contains
    /// entities from many modules, so each entity must be attributed to its owning module.
    /// <para>
    /// <paramref name="moduleByEntityType"/> maps each module's declared <c>DbSet&lt;T&gt;</c>
    /// entity CLR type to its module name. Entities that are only reachable through a navigation
    /// (e.g. a child collection whose element type has a key but no <c>DbSet</c>) are attributed
    /// to the same module as the principal they relate to, by following foreign keys. Any entity
    /// that still has no owning module (e.g. ASP.NET Identity tables, which ship no module
    /// <c>DbSet</c>) is assigned to <paramref name="identityModuleName"/> when one is provided.
    /// </para>
    /// <para>
    /// This is the fix for #229: previously a navigation-only child entity fell through the
    /// per-DbSet pass and was swept into the identity module's schema (e.g. <c>Users_RecurringLine</c>)
    /// instead of its real owner's (<c>Invoices_RecurringLine</c>), so the owning module's queries
    /// hit a "no such table" error.
    /// </para>
    /// </summary>
    public static void ApplyHostModuleSchemas(
        this ModelBuilder modelBuilder,
        DatabaseOptions dbOptions,
        IReadOnlyDictionary<Type, string> moduleByEntityType,
        string? identityModuleName
    )
    {
        var provider = DatabaseProviderDetector.Detect(
            dbOptions.DefaultConnection,
            dbOptions.Provider
        );

        // 1. Seed ownership from the modules' declared DbSet entity types.
        var moduleByEntity = new Dictionary<IMutableEntityType, string>();
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (moduleByEntityType.TryGetValue(entity.ClrType, out var module))
            {
                moduleByEntity[entity] = module;
            }
        }

        // 2. Propagate ownership across foreign keys so navigation-reachable child entities
        //    (which have no DbSet of their own) inherit their principal's module. Iterate to a
        //    fixed point so multi-level chains (a child of a child) are also covered.
        bool changed;
        do
        {
            changed = false;
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (moduleByEntity.ContainsKey(entity))
                {
                    continue;
                }

                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    if (moduleByEntity.TryGetValue(foreignKey.PrincipalEntityType, out var module))
                    {
                        moduleByEntity[entity] = module;
                        changed = true;
                        break;
                    }
                }
            }
        } while (changed);

        // 3. Apply the resolved module's schema/prefix; sweep anything still unowned into the
        //    identity module (whose own tables ship no module DbSet).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (!moduleByEntity.TryGetValue(entity, out var module))
            {
                if (identityModuleName is null)
                {
                    continue;
                }

                module = identityModuleName;
            }

            if (provider == DatabaseProvider.Sqlite)
            {
                var prefix = $"{module}_";
                var tableName = entity.GetTableName();
                if (
                    tableName is not null
                    && !tableName.StartsWith(prefix, StringComparison.Ordinal)
                )
                {
                    entity.SetTableName($"{prefix}{tableName}");
                }
            }
            else
            {
                entity.SetSchema(module.ToLowerInvariant());
            }
        }
    }
#pragma warning restore CA1308

    /// <summary>
    /// Applies multi-tenant query filters to all <see cref="IMultiTenant"/> entities.
    /// Call this from your DbContext's <c>OnModelCreating</c> after <see cref="ApplyModuleSchema"/>,
    /// passing the context itself (<c>this</c>).
    /// <para>
    /// The filter references <see cref="ITenantScopedDbContext.CurrentTenantId"/> on the
    /// <em>context instance</em>. EF Core caches the model per context type, but re-evaluates a
    /// reference to the executing context on every query — so each request is filtered by its own
    /// tenant. (A previous version captured the <see cref="ITenantContext"/> instance as a constant,
    /// which froze the first request's tenant into the cached model and leaked rows across tenants.)
    /// </para>
    /// <para>
    /// When <see cref="ITenantScopedDbContext.CurrentTenantId"/> is <c>null</c> (no tenant resolved),
    /// the filter matches only rows whose <c>TenantId</c> is also <c>null</c>. Store <c>null</c> on
    /// un-tenanted/global rows if they should be visible without a tenant; rows with a non-null
    /// tenant are hidden.
    /// </para>
    /// <example>
    /// <code>
    /// public sealed class ProductsDbContext(DbContextOptions&lt;ProductsDbContext&gt; options,
    ///     ITenantContext tenant) : DbContext(options), ITenantScopedDbContext
    /// {
    ///     public string? CurrentTenantId =&gt; tenant.TenantId;
    ///
    ///     protected override void OnModelCreating(ModelBuilder modelBuilder)
    ///     {
    ///         modelBuilder.ApplyModuleSchema("Products", dbOptions.Value);
    ///         modelBuilder.ApplyMultiTenantFilters(this);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public static void ApplyMultiTenantFilters<TContext>(
        this ModelBuilder modelBuilder,
        TContext context
    )
        where TContext : DbContext, ITenantScopedDbContext
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(IMultiTenant.TenantId));

            // Reference the current tenant via the executing DbContext instance so EF
            // re-evaluates it per query instead of freezing a captured instance.
            var currentTenantId = Expression.Property(
                Expression.Constant(context),
                nameof(ITenantScopedDbContext.CurrentTenantId)
            );

            var filter = Expression.Lambda(
                Expression.Equal(tenantIdProperty, currentTenantId),
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
