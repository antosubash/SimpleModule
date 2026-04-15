using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class AgentFinder
{
    internal static void FindImplementors(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol interfaceSymbol,
        string moduleName,
        List<DiscoveredTypeInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindImplementors(childNamespace, interfaceSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && !typeSymbol.IsAbstract
                && typeSymbol.TypeKind == TypeKind.Class
                && SymbolHelpers.ImplementsInterface(typeSymbol, interfaceSymbol)
            )
            {
                results.Add(
                    new DiscoveredTypeInfo
                    {
                        FullyQualifiedName = typeSymbol.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ),
                        ModuleName = moduleName,
                    }
                );
            }
        }
    }
}
