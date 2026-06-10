using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PolicyFinder
{
    internal static void FindPolicyTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol policyInterfaceSymbol,
        INamedTypeSymbol? dtoAttributeSymbol,
        Dictionary<string, string> contractsAssemblyMap,
        Dictionary<string, string> moduleAssemblyMap,
        string moduleName,
        List<PolicyInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindPolicyTypes(
                    childNs,
                    policyInterfaceSymbol,
                    dtoAttributeSymbol,
                    contractsAssemblyMap,
                    moduleAssemblyMap,
                    moduleName,
                    results
                );
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                InspectType(
                    typeSymbol,
                    policyInterfaceSymbol,
                    dtoAttributeSymbol,
                    contractsAssemblyMap,
                    moduleAssemblyMap,
                    moduleName,
                    results
                );
            }
        }
    }

    private static void InspectType(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol policyInterfaceSymbol,
        INamedTypeSymbol? dtoAttributeSymbol,
        Dictionary<string, string> contractsAssemblyMap,
        Dictionary<string, string> moduleAssemblyMap,
        string moduleName,
        List<PolicyInfo> results
    )
    {
        if (
            typeSymbol.TypeKind == TypeKind.Class
            && !typeSymbol.IsAbstract
            && !typeSymbol.IsStatic
        )
        {
            // A class may implement IPolicy<T> for more than one resource type;
            // each closed interface becomes its own DI registration.
            foreach (var iface in typeSymbol.AllInterfaces)
            {
                if (
                    !SymbolEqualityComparer.Default.Equals(
                        iface.OriginalDefinition,
                        policyInterfaceSymbol
                    )
                )
                    continue;

                var resourceType = iface.TypeArguments[0];

                results.Add(
                    new PolicyInfo
                    {
                        FullyQualifiedName = typeSymbol.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ),
                        ResourceTypeFqn = resourceType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ),
                        ModuleName = moduleName,
                        IsPublic = typeSymbol.DeclaredAccessibility == Accessibility.Public,
                        ResourceIsContractsDto = IsContractsDto(resourceType, dtoAttributeSymbol),
                        ResourceModuleName = ResolveResourceModule(
                            resourceType,
                            contractsAssemblyMap,
                            moduleAssemblyMap
                        ),
                        Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                    }
                );
            }
        }

        // Policies may be declared as nested classes — recurse into type members.
        foreach (var nested in typeSymbol.GetTypeMembers())
        {
            InspectType(
                nested,
                policyInterfaceSymbol,
                dtoAttributeSymbol,
                contractsAssemblyMap,
                moduleAssemblyMap,
                moduleName,
                results
            );
        }
    }

    /// <summary>
    /// A valid policy resource is a contracts DTO: either marked [Dto] or declared in a
    /// .Contracts assembly. Checked symbolically (not via the DtoTypes list) so contracts
    /// entities excluded from TS/JSON generation ([NoDtoGeneration], IEvent) still qualify.
    /// </summary>
    private static bool IsContractsDto(ITypeSymbol resourceType, INamedTypeSymbol? dtoAttribute)
    {
        if (
            resourceType.ContainingAssembly?.Name.EndsWith(
                ".Contracts",
                StringComparison.OrdinalIgnoreCase
            ) == true
        )
        {
            return true;
        }

        if (dtoAttribute is null)
            return false;

        foreach (var attribute in resourceType.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, dtoAttribute))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves which module owns the resource type via its containing assembly.
    /// Returns "" when the assembly maps to no known module (host types, framework types).
    /// </summary>
    private static string ResolveResourceModule(
        ITypeSymbol resourceType,
        Dictionary<string, string> contractsAssemblyMap,
        Dictionary<string, string> moduleAssemblyMap
    )
    {
        var assemblyName = resourceType.ContainingAssembly?.Name;
        if (assemblyName is null)
            return "";

        if (contractsAssemblyMap.TryGetValue(assemblyName, out var contractsModule))
            return contractsModule;

        if (moduleAssemblyMap.TryGetValue(assemblyName, out var implModule))
            return implModule;

        return "";
    }

    /// <summary>
    /// Scans every module's implementation assembly and every contracts assembly for
    /// IPolicy&lt;T&gt; implementors. No-op when the policy interface isn't resolvable.
    /// </summary>
    internal static void Discover(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Dictionary<string, IAssemblySymbol> contractsAssemblySymbols,
        Dictionary<string, string> contractsAssemblyMap,
        Dictionary<string, string> moduleAssemblyMap,
        CoreSymbols symbols,
        List<PolicyInfo> policies
    )
    {
        if (symbols.PolicyInterface is null)
            return;

        SymbolHelpers.ScanModuleAssemblies(
            modules,
            moduleSymbols,
            (assembly, module) =>
            {
                FindPolicyTypes(
                    assembly.GlobalNamespace,
                    symbols.PolicyInterface,
                    symbols.DtoAttribute,
                    contractsAssemblyMap,
                    moduleAssemblyMap,
                    module.ModuleName,
                    policies
                );
            }
        );

        foreach (var kvp in contractsAssemblySymbols)
        {
            if (contractsAssemblyMap.TryGetValue(kvp.Key, out var moduleName))
            {
                FindPolicyTypes(
                    kvp.Value.GlobalNamespace,
                    symbols.PolicyInterface,
                    symbols.DtoAttribute,
                    contractsAssemblyMap,
                    moduleAssemblyMap,
                    moduleName,
                    policies
                );
            }
        }
    }
}
