using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DependencyChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // SM0010: Circular module dependency
        var (_, sortResult) = TopologicalSort.SortModulesWithResult(data);

        if (!sortResult.IsSuccess && sortResult.Cycle.Length > 0)
        {
            // Build cycle string: "A → B → C → A"
            var cycleNodes = new List<string>();
            foreach (var c in sortResult.Cycle)
                cycleNodes.Add(c);
            cycleNodes.Add(sortResult.Cycle[0]); // close the loop
            var cycleStr = string.Join(" \u2192 ", cycleNodes);

            // Build "how it happened" string
            var cycleSet = new HashSet<string>();
            foreach (var c in sortResult.Cycle)
                cycleSet.Add(c);

            var howParts = new List<string>();
            foreach (var dep in data.Dependencies)
            {
                if (cycleSet.Contains(dep.ModuleName) && cycleSet.Contains(dep.DependsOnModuleName))
                {
                    howParts.Add(
                        dep.ModuleName + " references " + dep.ContractsAssemblyName + ". "
                    );
                }
            }
            var howStr = string.Join("", howParts);

            var first = sortResult.Cycle[0];
            var second = sortResult.Cycle.Length > 1 ? sortResult.Cycle[1] : first;

            // Find location of the first module in the cycle
            SourceLocationRecord? cycleLoc = null;
            foreach (var module in data.Modules)
            {
                if (module.ModuleName == first)
                {
                    cycleLoc = module.Location;
                    break;
                }
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.CircularModuleDependency,
                    LocationHelper.ToLocation(cycleLoc),
                    cycleStr,
                    howStr,
                    first,
                    second
                )
            );
        }

        // SM0011: Illegal implementation references
        foreach (var illegal in data.IllegalReferences)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.IllegalImplementationReference,
                    LocationHelper.ToLocation(illegal.Location),
                    illegal.ReferencingModuleName,
                    illegal.ReferencedModuleName,
                    illegal.ReferencedAssemblyName
                )
            );
        }
    }
}
