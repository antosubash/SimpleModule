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
}
