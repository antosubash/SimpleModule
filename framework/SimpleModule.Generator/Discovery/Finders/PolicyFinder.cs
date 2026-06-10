using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PolicyFinder
{
    /// <summary>
    /// Invariant inputs for one policy scan, bundled so the recursive walk doesn't
    /// thread two adjacent same-typed dictionaries through every call site.
    /// </summary>
    private readonly struct PolicyScanContext(
        INamedTypeSymbol policyInterface,
        INamedTypeSymbol? dtoAttribute,
        Dictionary<string, string> contractsAssemblyMap,
        Dictionary<string, string> moduleAssemblyMap,
        List<PolicyInfo> results
    )
    {
        public INamedTypeSymbol PolicyInterface { get; } = policyInterface;
        public INamedTypeSymbol? DtoAttribute { get; } = dtoAttribute;
        public Dictionary<string, string> ContractsAssemblyMap { get; } = contractsAssemblyMap;
        public Dictionary<string, string> ModuleAssemblyMap { get; } = moduleAssemblyMap;
        public List<PolicyInfo> Results { get; } = results;
    }

    private static void FindPolicyTypes(
        INamespaceSymbol namespaceSymbol,
        in PolicyScanContext context,
        string moduleName
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindPolicyTypes(childNs, context, moduleName);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                InspectType(typeSymbol, context, moduleName);
            }
        }
    }

    private static void InspectType(
        INamedTypeSymbol typeSymbol,
        in PolicyScanContext context,
        string moduleName
    )
    {
        // Policies can only be classes; pruning here also keeps the nested-type
        // recursion below from visiting struct/enum/delegate members.
        if (typeSymbol.TypeKind != TypeKind.Class)
            return;

        if (!typeSymbol.IsAbstract && !typeSymbol.IsStatic)
        {
            // A class may implement IPolicy<T> for more than one resource type;
            // each closed interface becomes its own DI registration.
            foreach (var iface in typeSymbol.AllInterfaces)
            {
                if (
                    !SymbolEqualityComparer.Default.Equals(
                        iface.OriginalDefinition,
                        context.PolicyInterface
                    )
                )
                    continue;

                var resourceType = iface.TypeArguments[0];

                context.Results.Add(
                    new PolicyInfo
                    {
                        FullyQualifiedName = typeSymbol.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ),
                        ResourceTypeFqn = resourceType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ),
                        ModuleName = moduleName,
                        // Effective accessibility: a public class nested inside a
                        // non-public outer type is unreachable from generated code.
                        IsPublic = IsEffectivelyPublic(typeSymbol),
                        IsGeneric = typeSymbol.IsGenericType,
                        IsManuallyRegistered = ContractFinder.HasManualRegistrationAttribute(
                            typeSymbol
                        ),
                        ResourceIsTypeParameter = resourceType is ITypeParameterSymbol,
                        ResourceIsContractsDto = IsContractsDto(
                            resourceType,
                            context.DtoAttribute
                        ),
                        ResourceModuleName = ResolveResourceModule(resourceType, context),
                        Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                    }
                );
            }
        }

        // Policies may be declared as nested classes — recurse into type members.
        foreach (var nested in typeSymbol.GetTypeMembers())
        {
            InspectType(nested, context, moduleName);
        }
    }

    private static bool IsEffectivelyPublic(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
                return false;
        }
        return true;
    }

    /// <summary>
    /// A valid policy resource is an effectively-public contracts DTO: either marked
    /// [Dto] or declared in a .Contracts assembly. Checked symbolically (not via the
    /// DtoTypes list) so contracts entities excluded from TS/JSON generation
    /// ([NoDtoGeneration], IEvent) still qualify. Non-public resources are rejected
    /// because the generated registration could not reference them. The suffix match
    /// is case-insensitive to agree with SymbolDiscovery's contracts classification.
    /// </summary>
    private static bool IsContractsDto(ITypeSymbol resourceType, INamedTypeSymbol? dtoAttribute)
    {
        if (resourceType is not INamedTypeSymbol named || !IsEffectivelyPublic(named))
            return false;

        if (
            named.ContainingAssembly?.Name.EndsWith(
                AssemblyConventions.ContractsSuffix,
                StringComparison.OrdinalIgnoreCase
            ) == true
        )
        {
            return true;
        }

        if (dtoAttribute is null)
            return false;

        foreach (var attribute in named.GetAttributes())
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
        in PolicyScanContext context
    )
    {
        var assemblyName = resourceType.ContainingAssembly?.Name;
        if (assemblyName is null)
            return "";

        if (context.ContractsAssemblyMap.TryGetValue(assemblyName, out var contractsModule))
            return contractsModule;

        if (context.ModuleAssemblyMap.TryGetValue(assemblyName, out var implModule))
            return implModule;

        return "";
    }

    /// <summary>
    /// Scans every module implementation assembly, every contracts assembly (mapped to a
    /// module or not), and the compiling (host) assembly for IPolicy&lt;T&gt; implementors,
    /// visiting each assembly exactly once. No-op when the policy interface isn't
    /// resolvable.
    /// </summary>
    internal static void Discover(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        IReadOnlyList<IAssemblySymbol> contractsAssemblies,
        Dictionary<string, string> contractsAssemblyMap,
        Dictionary<string, string> moduleAssemblyMap,
        IAssemblySymbol hostAssembly,
        CoreSymbols symbols,
        List<PolicyInfo> policies
    )
    {
        if (symbols.PolicyInterface is null)
            return;

        var context = new PolicyScanContext(
            symbols.PolicyInterface,
            symbols.DtoAttribute,
            contractsAssemblyMap,
            moduleAssemblyMap,
            policies
        );

        var scanned = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        foreach (var module in modules)
        {
            if (
                moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol)
                && scanned.Add(typeSymbol.ContainingAssembly)
            )
            {
                FindPolicyTypes(
                    typeSymbol.ContainingAssembly.GlobalNamespace,
                    context,
                    module.ModuleName
                );
            }
        }

        // All contracts assemblies — including ones whose name maps to no module, so a
        // policy there is still registered (its SM0060 ownership check is skipped).
        foreach (var contractsAssembly in contractsAssemblies)
        {
            if (scanned.Add(contractsAssembly))
            {
                contractsAssemblyMap.TryGetValue(contractsAssembly.Name, out var moduleName);
                FindPolicyTypes(contractsAssembly.GlobalNamespace, context, moduleName ?? "");
            }
        }

        // The compiling assembly: hosts may declare policies too (already covered when
        // the host itself contains a [Module] class).
        if (scanned.Add(hostAssembly))
        {
            moduleAssemblyMap.TryGetValue(hostAssembly.Name, out var hostModule);
            FindPolicyTypes(hostAssembly.GlobalNamespace, context, hostModule ?? "");
        }
    }
}
