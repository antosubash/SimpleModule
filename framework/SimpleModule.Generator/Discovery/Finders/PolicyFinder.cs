using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PolicyFinder
{
    internal static void FindPolicyTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol policyInterfaceSymbol,
        string moduleName,
        List<PolicyInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindPolicyTypes(childNs, policyInterfaceSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
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

                    results.Add(
                        new PolicyInfo
                        {
                            FullyQualifiedName = typeSymbol.ToDisplayString(
                                SymbolDisplayFormat.FullyQualifiedFormat
                            ),
                            ResourceTypeFqn = iface.TypeArguments[0]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            ModuleName = moduleName,
                            IsPublic =
                                typeSymbol.DeclaredAccessibility == Accessibility.Public,
                            Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                        }
                    );
                }
            }
        }
    }

    /// <summary>
    /// Scans every module's implementation assembly for IPolicy&lt;T&gt; implementors.
    /// No-op when the policy interface isn't resolvable.
    /// </summary>
    internal static void Discover(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        CoreSymbols symbols,
        List<PolicyInfo> policies
    )
    {
        if (symbols.PolicyInterface is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                {
                    FindPolicyTypes(
                        assembly.GlobalNamespace,
                        symbols.PolicyInterface,
                        module.ModuleName,
                        policies
                    );
                }
            );
        }
    }
}
