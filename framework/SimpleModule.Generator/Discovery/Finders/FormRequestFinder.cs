using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class FormRequestFinder
{
    internal static void Discover(
        IReadOnlyList<IAssemblySymbol> refAssemblies,
        INamespaceSymbol hostGlobalNamespace,
        CoreSymbols symbols,
        List<FormRequestInfo> formRequests,
        CancellationToken cancellationToken
    )
    {
        if (symbols.FormRequestAttribute is null)
            return;

        foreach (var assemblySymbol in refAssemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FindFormRequestTypes(
                assemblySymbol.GlobalNamespace,
                symbols,
                formRequests,
                cancellationToken
            );
        }

        FindFormRequestTypes(hostGlobalNamespace, symbols, formRequests, cancellationToken);
    }

    private static void FindFormRequestTypes(
        INamespaceSymbol namespaceSymbol,
        CoreSymbols symbols,
        List<FormRequestInfo> formRequests,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindFormRequestTypes(childNamespace, symbols, formRequests, cancellationToken);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                var hasAttribute = false;
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            attr.AttributeClass,
                            symbols.FormRequestAttribute
                        )
                    )
                    {
                        hasAttribute = true;
                        break;
                    }
                }

                if (!hasAttribute)
                    continue;

                var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                var extendsFormRequest = false;
                var current = typeSymbol.BaseType;
                while (current is not null)
                {
                    if (
                        symbols.FormRequestBase is not null
                        && SymbolEqualityComparer.Default.Equals(
                            current.OriginalDefinition,
                            symbols.FormRequestBase
                        )
                    )
                    {
                        extendsFormRequest = true;
                        break;
                    }
                    current = current.BaseType;
                }

                formRequests.Add(
                    new FormRequestInfo
                    {
                        FullyQualifiedName = fqn,
                        IsSealed = typeSymbol.IsSealed,
                        ExtendsFormRequest = extendsFormRequest,
                        Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                    }
                );
            }
        }
    }
}
