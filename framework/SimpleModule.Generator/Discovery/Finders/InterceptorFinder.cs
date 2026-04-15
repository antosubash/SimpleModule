using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class InterceptorFinder
{
    internal static void FindInterceptorTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol saveChangesInterceptorSymbol,
        string moduleName,
        List<InterceptorInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindInterceptorTypes(childNs, saveChangesInterceptorSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
                && SymbolHelpers.ImplementsInterface(typeSymbol, saveChangesInterceptorSymbol)
            )
            {
                var info = new InterceptorInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                };

                // Extract constructor parameter type FQNs
                foreach (var ctor in typeSymbol.Constructors)
                {
                    if (ctor.DeclaredAccessibility != Accessibility.Public)
                        continue;

                    foreach (var param in ctor.Parameters)
                    {
                        info.ConstructorParamTypeFqns.Add(
                            param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        );
                    }

                    // Only process the first public constructor
                    break;
                }

                results.Add(info);
            }
        }
    }
}
