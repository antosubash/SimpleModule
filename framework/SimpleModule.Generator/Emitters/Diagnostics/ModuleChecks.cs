using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class ModuleChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // SM0002: Empty module name
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EmptyModuleName,
                        LocationHelper.ToLocation(module.Location),
                        Strip(module.FullyQualifiedName)
                    )
                );
            }
        }

        // SM0040: Duplicate module name
        var seenModuleNames = new Dictionary<string, string>();
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
                continue;

            if (seenModuleNames.TryGetValue(module.ModuleName, out var existingFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateModuleName,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName,
                        Strip(existingFqn),
                        Strip(module.FullyQualifiedName)
                    )
                );
            }
            else
            {
                seenModuleNames[module.ModuleName] = module.FullyQualifiedName;
            }
        }

        // SM0043: Empty module (no IModule methods overridden)
        var moduleNamesWithDbContext = new HashSet<string>(
            data.DbContexts.Select(db => db.ModuleName),
            System.StringComparer.Ordinal
        );
        foreach (var module in data.Modules)
        {
            if (
                !module.HasConfigureServices
                && !module.HasConfigureEndpoints
                && !module.HasConfigureMenu
                && !module.HasConfigurePermissions
                && !module.HasConfigureMiddleware
                && !module.HasConfigureSettings
                && !module.HasConfigureFeatureFlags
                && module.Endpoints.Length == 0
                && module.Views.Length == 0
                && !moduleNamesWithDbContext.Contains(module.ModuleName)
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EmptyModuleWarning,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName
                    )
                );
            }
        }
    }

    private static string Strip(string fqn) => TypeMappingHelpers.StripGlobalPrefix(fqn);
}
