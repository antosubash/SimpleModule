using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DbContextFinder
{
    internal static void FindDbContextTypes(
        INamespaceSymbol namespaceSymbol,
        string moduleName,
        List<DbContextInfo> dbContexts,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindDbContextTypes(childNamespace, moduleName, dbContexts, cancellationToken);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
            )
            {
                // Walk base type chain looking for DbContext
                var isDbContext = false;
                var isIdentity = false;
                string identityUserFqn = "";
                string identityRoleFqn = "";
                string identityKeyFqn = "";

                var current = typeSymbol.BaseType;
                while (current is not null)
                {
                    var baseFqn = current.OriginalDefinition.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );

                    if (
                        baseFqn
                        == "global::Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<TUser, TRole, TKey>"
                    )
                    {
                        isDbContext = true;
                        isIdentity = true;
                        if (current.TypeArguments.Length >= 3)
                        {
                            identityUserFqn = current
                                .TypeArguments[0]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            identityRoleFqn = current
                                .TypeArguments[1]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            identityKeyFqn = current
                                .TypeArguments[2]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        }
                        break;
                    }

                    if (baseFqn == "global::Microsoft.EntityFrameworkCore.DbContext")
                    {
                        isDbContext = true;
                        break;
                    }

                    current = current.BaseType;
                }

                if (!isDbContext)
                    continue;

                var info = new DbContextInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsIdentityDbContext = isIdentity,
                    IdentityUserTypeFqn = identityUserFqn,
                    IdentityRoleTypeFqn = identityRoleFqn,
                    IdentityKeyTypeFqn = identityKeyFqn,
                    Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                };

                // Collect DbSet<T> properties
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (
                        m is IPropertySymbol prop
                        && prop.DeclaredAccessibility == Accessibility.Public
                        && !prop.IsStatic
                        && prop.Type is INamedTypeSymbol propType
                        && propType.IsGenericType
                        && propType.OriginalDefinition.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ) == "global::Microsoft.EntityFrameworkCore.DbSet<TEntity>"
                    )
                    {
                        var entityType = propType.TypeArguments[0];
                        var entityFqn = entityType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        );
                        var entityAssemblyName =
                            entityType.ContainingAssembly?.Name ?? string.Empty;
                        info.DbSets.Add(
                            new DbSetInfo
                            {
                                PropertyName = prop.Name,
                                EntityFqn = entityFqn,
                                EntityAssemblyName = entityAssemblyName,
                                EntityLocation = SymbolHelpers.GetSourceLocation(entityType),
                            }
                        );
                    }
                }

                dbContexts.Add(info);
            }
        }
    }

    internal static void FindEntityConfigTypes(
        INamespaceSymbol namespaceSymbol,
        string moduleName,
        List<EntityConfigInfo> entityConfigs,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindEntityConfigTypes(childNamespace, moduleName, entityConfigs, cancellationToken);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
            )
            {
                foreach (var iface in typeSymbol.AllInterfaces)
                {
                    if (
                        iface.IsGenericType
                        && iface.OriginalDefinition.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        )
                            == "global::Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<TEntity>"
                    )
                    {
                        var entityFqn = iface
                            .TypeArguments[0]
                            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        entityConfigs.Add(
                            new EntityConfigInfo
                            {
                                ConfigFqn = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                EntityFqn = entityFqn,
                                ModuleName = moduleName,
                                Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// For each module, scans the module's own assembly (once per unique
    /// assembly) and distributes every discovered DbContext / EntityTypeConfiguration
    /// to the module whose namespace is closest.
    /// </summary>
    internal static void Discover(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        List<DbContextInfo> dbContexts,
        List<EntityConfigInfo> entityConfigs,
        CancellationToken cancellationToken
    )
    {
        var scannedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var module in modules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var assembly = typeSymbol.ContainingAssembly;
            if (!scannedAssemblies.Add(assembly))
                continue;

            // Collect unmatched items from this assembly
            var rawDbContexts = new List<DbContextInfo>();
            var rawEntityConfigs = new List<EntityConfigInfo>();
            FindDbContextTypes(assembly.GlobalNamespace, "", rawDbContexts, cancellationToken);
            FindEntityConfigTypes(
                assembly.GlobalNamespace,
                "",
                rawEntityConfigs,
                cancellationToken
            );

            // Match each DbContext to the module whose namespace is closest
            foreach (var ctx in rawDbContexts)
            {
                var ctxNs = TypeMappingHelpers.StripGlobalPrefix(ctx.FullyQualifiedName);
                ctx.ModuleName = SymbolHelpers.FindClosestModuleName(ctxNs, modules);
                dbContexts.Add(ctx);
            }

            foreach (var cfg in rawEntityConfigs)
            {
                var cfgNs = TypeMappingHelpers.StripGlobalPrefix(cfg.ConfigFqn);
                cfg.ModuleName = SymbolHelpers.FindClosestModuleName(cfgNs, modules);
                entityConfigs.Add(cfg);
            }
        }
    }

    internal static bool HasDbContextConstructorParam(INamedTypeSymbol typeSymbol)
    {
        foreach (var ctor in typeSymbol.Constructors)
        {
            if (ctor.DeclaredAccessibility != Accessibility.Public || ctor.IsStatic)
                continue;

            foreach (var param in ctor.Parameters)
            {
                var paramType = param.Type;
                // Walk the base type chain to check for DbContext ancestry
                var current = paramType.BaseType;
                while (current != null)
                {
                    var baseFqn = current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (
                        baseFqn == "global::Microsoft.EntityFrameworkCore.DbContext"
                        || baseFqn.StartsWith(
                            "global::Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext",
                            StringComparison.Ordinal
                        )
                    )
                    {
                        return true;
                    }

                    current = current.BaseType;
                }
            }
        }

        return false;
    }
}
