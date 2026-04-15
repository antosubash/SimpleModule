using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DbContextChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // SM0003: Multiple IdentityDbContexts
        DbContextInfoRecord? firstIdentity = null;
        foreach (var ctx in data.DbContexts)
        {
            if (!ctx.IsIdentityDbContext)
                continue;

            if (firstIdentity is null)
            {
                firstIdentity = ctx;
            }
            else
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MultipleIdentityDbContexts,
                        LocationHelper.ToLocation(ctx.Location),
                        Strip(firstIdentity.Value.FullyQualifiedName),
                        firstIdentity.Value.ModuleName,
                        Strip(ctx.FullyQualifiedName),
                        ctx.ModuleName
                    )
                );
            }
        }

        // SM0005: IdentityDbContext with wrong type args
        foreach (var ctx in data.DbContexts)
        {
            if (ctx.IsIdentityDbContext && string.IsNullOrEmpty(ctx.IdentityUserTypeFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.IdentityDbContextBadTypeArgs,
                        LocationHelper.ToLocation(ctx.Location),
                        Strip(ctx.FullyQualifiedName),
                        ctx.ModuleName,
                        0
                    )
                );
            }
        }

        // SM0055: Entity classes must live in a .Contracts assembly.
        // Walks every DbSet in the same pass that also collects EntityFqns
        // for SM0006 below, so we only iterate data.DbContexts once.
        var allEntityFqns = new HashSet<string>();
        foreach (var ctx in data.DbContexts)
        {
            foreach (var dbSet in ctx.DbSets)
            {
                allEntityFqns.Add(dbSet.EntityFqn);

                // Skip entities we can't flag: IdentityDbContext external types,
                // metadata-only symbols (no source location), and anything that
                // lives outside the SimpleModule.* assembly family.
                if (ctx.IsIdentityDbContext)
                    continue;
                if (dbSet.EntityLocation is null)
                    continue;
                if (
                    !dbSet.EntityAssemblyName.StartsWith(
                        AssemblyConventions.FrameworkPrefix,
                        StringComparison.Ordinal
                    )
                )
                    continue;
                if (
                    dbSet.EntityAssemblyName.EndsWith(
                        AssemblyConventions.ContractsSuffix,
                        StringComparison.Ordinal
                    )
                )
                    continue;

                var expectedContractsAssembly =
                    AssemblyConventions.GetExpectedContractsAssemblyName(dbSet.EntityAssemblyName);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EntityNotInContractsAssembly,
                        LocationHelper.ToLocation(dbSet.EntityLocation),
                        Strip(dbSet.EntityFqn),
                        dbSet.PropertyName,
                        Strip(ctx.FullyQualifiedName),
                        dbSet.EntityAssemblyName,
                        expectedContractsAssembly
                    )
                );
            }
        }

        // SM0006: Entity config for entity not in any DbSet
        // (allEntityFqns was populated above during the SM0055 pass)
        foreach (var config in data.EntityConfigs)
        {
            if (!allEntityFqns.Contains(config.EntityFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EntityConfigForMissingEntity,
                        LocationHelper.ToLocation(config.Location),
                        Strip(config.EntityFqn),
                        Strip(config.ConfigFqn),
                        config.ModuleName
                    )
                );
            }
        }

        // SM0007: Duplicate EntityTypeConfiguration for same entity
        var entityConfigOwners = new Dictionary<string, string>();
        foreach (var config in data.EntityConfigs)
        {
            if (entityConfigOwners.TryGetValue(config.EntityFqn, out var existing))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateEntityConfiguration,
                        LocationHelper.ToLocation(config.Location),
                        Strip(config.EntityFqn),
                        existing,
                        Strip(config.ConfigFqn)
                    )
                );
            }
            else
            {
                entityConfigOwners[config.EntityFqn] = Strip(config.ConfigFqn);
            }
        }
    }

    private static string Strip(string fqn) => TypeMappingHelpers.StripGlobalPrefix(fqn);
}
