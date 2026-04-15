using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class ContractFinder
{
    internal static void ScanContractInterfaces(
        INamespaceSymbol namespaceSymbol,
        string assemblyName,
        List<ContractInterfaceInfoRecord> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                ScanContractInterfaces(childNs, assemblyName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Interface
                && typeSymbol.DeclaredAccessibility == Accessibility.Public
            )
            {
                var methodCount = 0;
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (m is IMethodSymbol ms && ms.MethodKind == MethodKind.Ordinary)
                        methodCount++;
                }

                results.Add(
                    new ContractInterfaceInfoRecord(
                        assemblyName,
                        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        methodCount,
                        SymbolHelpers.GetSourceLocation(typeSymbol)
                    )
                );
            }
        }
    }

    internal static void FindContractImplementations(
        INamespaceSymbol namespaceSymbol,
        HashSet<string> contractInterfaceFqns,
        string moduleName,
        Compilation compilation,
        List<ContractImplementationInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindContractImplementations(
                    childNs,
                    contractInterfaceFqns,
                    moduleName,
                    compilation,
                    results
                );
            }
            else if (member is INamedTypeSymbol typeSymbol && typeSymbol.TypeKind == TypeKind.Class)
            {
                foreach (var iface in typeSymbol.AllInterfaces)
                {
                    var ifaceFqn = iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (contractInterfaceFqns.Contains(ifaceFqn))
                    {
                        results.Add(
                            new ContractImplementationInfo
                            {
                                InterfaceFqn = ifaceFqn,
                                ImplementationFqn = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                ModuleName = moduleName,
                                IsPublic = typeSymbol.DeclaredAccessibility == Accessibility.Public,
                                IsAbstract = typeSymbol.IsAbstract,
                                DependsOnDbContext = DbContextFinder.HasDbContextConstructorParam(
                                    typeSymbol
                                ),
                                Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                                Lifetime = GetContractLifetime(typeSymbol),
                            }
                        );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reads [ContractLifetime(ServiceLifetime.X)] from the type.
    /// Returns 1 (Scoped) if the attribute is not present.
    /// ServiceLifetime: Singleton=0, Scoped=1, Transient=2
    /// </summary>
    internal static int GetContractLifetime(INamedTypeSymbol typeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            if (
                attrName == "global::SimpleModule.Core.ContractLifetimeAttribute"
                && attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is int lifetime
            )
            {
                return lifetime;
            }
        }
        return 1; // Default: Scoped
    }

    /// <summary>
    /// Maps every pre-classified *.Contracts assembly to the module it belongs to.
    /// Populates <paramref name="contractsAssemblyMap"/> (name → module name) and
    /// <paramref name="contractsAssemblySymbols"/> (name → IAssemblySymbol) for
    /// downstream scans.
    /// </summary>
    internal static void BuildContractsAssemblyMap(
        IReadOnlyList<IAssemblySymbol> contractsAssemblies,
        Dictionary<string, string> moduleAssemblyMap,
        Dictionary<string, string> contractsAssemblyMap,
        Dictionary<string, IAssemblySymbol> contractsAssemblySymbols
    )
    {
        foreach (var asm in contractsAssemblies)
        {
            var asmName = asm.Name;
            var baseName = asmName.Substring(0, asmName.Length - ".Contracts".Length);

            // Try exact match on assembly name
            if (moduleAssemblyMap.TryGetValue(baseName, out var moduleName))
            {
                contractsAssemblyMap[asmName] = moduleName;
                contractsAssemblySymbols[asmName] = asm;
                continue;
            }

            // Try matching last segment of baseName to module names (case-insensitive)
            var lastDot = baseName.LastIndexOf('.');
            var lastSegment = lastDot >= 0 ? baseName.Substring(lastDot + 1) : baseName;

            foreach (var kvp in moduleAssemblyMap)
            {
                if (string.Equals(lastSegment, kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    contractsAssemblyMap[asmName] = kvp.Value;
                    contractsAssemblySymbols[asmName] = asm;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Walks every contracts assembly and records its public interfaces; then walks
    /// each module's own assembly and records classes that implement any of those
    /// interfaces. Populates <paramref name="contractInterfaces"/> and
    /// <paramref name="contractImplementations"/>.
    /// </summary>
    internal static void DiscoverInterfacesAndImplementations(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Dictionary<string, IAssemblySymbol> contractsAssemblySymbols,
        Compilation compilation,
        List<ContractInterfaceInfoRecord> contractInterfaces,
        List<ContractImplementationInfo> contractImplementations
    )
    {
        // Step 3: Scan contract interfaces
        foreach (var kvp in contractsAssemblySymbols)
        {
            ScanContractInterfaces(kvp.Value.GlobalNamespace, kvp.Key, contractInterfaces);
        }

        // Step 3b: Find implementations of contract interfaces in module assemblies
        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var moduleAssembly = typeSymbol.ContainingAssembly;

            // Find which contracts assembly this module owns
            var expectedContractsAsm = moduleAssembly.Name + ".Contracts";
            if (!contractsAssemblySymbols.ContainsKey(expectedContractsAsm))
                continue;

            // Get the interface FQNs from this module's contracts
            var moduleContractInterfaceFqns = new HashSet<string>();
            foreach (var ci in contractInterfaces)
            {
                if (ci.ContractsAssemblyName == expectedContractsAsm)
                    moduleContractInterfaceFqns.Add(ci.InterfaceName);
            }
            if (moduleContractInterfaceFqns.Count == 0)
                continue;

            // Scan module assembly for implementations
            FindContractImplementations(
                moduleAssembly.GlobalNamespace,
                moduleContractInterfaceFqns,
                module.ModuleName,
                compilation,
                contractImplementations
            );
        }
    }
}
