using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DependencyAnalyzer
{
    /// <summary>
    /// Walks each module's referenced assemblies and classifies them:
    /// <list type="bullet">
    ///   <item>A reference to another module's *implementation* assembly is illegal
    ///   and added to <paramref name="illegalReferences"/>.</item>
    ///   <item>A reference to another module's *.Contracts* assembly is a normal
    ///   dependency and added to <paramref name="dependencies"/>.</item>
    /// </list>
    /// </summary>
    internal static void Analyze(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Dictionary<string, string> moduleAssemblyMap,
        Dictionary<string, string> contractsAssemblyMap,
        List<ModuleDependencyRecord> dependencies,
        List<IllegalModuleReferenceRecord> illegalReferences
    )
    {
        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var moduleAssembly = typeSymbol.ContainingAssembly;
            var thisModuleName = module.ModuleName;

            foreach (var asmModule in moduleAssembly.Modules)
            {
                foreach (var referencedAsm in asmModule.ReferencedAssemblySymbols)
                {
                    var refName = referencedAsm.Name;

                    // Check for illegal direct module-to-module reference
                    if (
                        moduleAssemblyMap.TryGetValue(refName, out var referencedModuleName)
                        && !string.Equals(
                            referencedModuleName,
                            thisModuleName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        illegalReferences.Add(
                            new IllegalModuleReferenceRecord(
                                thisModuleName,
                                moduleAssembly.Name,
                                referencedModuleName,
                                refName,
                                module.Location
                            )
                        );
                    }

                    // Check for dependency via contracts
                    if (
                        contractsAssemblyMap.TryGetValue(refName, out var depModuleName)
                        && !string.Equals(
                            depModuleName,
                            thisModuleName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        dependencies.Add(
                            new ModuleDependencyRecord(thisModuleName, depModuleName, refName)
                        );
                    }
                }
            }
        }
    }
}
