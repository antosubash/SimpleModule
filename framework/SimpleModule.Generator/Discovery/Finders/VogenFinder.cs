using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class VogenFinder
{
    internal static void FindVogenValueObjectsWithEfConverters(
        INamespaceSymbol ns,
        List<VogenValueObjectRecord> results
    )
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (!IsVogenValueObject(type))
                continue;

            var converterMembers = type.GetTypeMembers("EfCoreValueConverter");
            var comparerMembers = type.GetTypeMembers("EfCoreValueComparer");

            if (converterMembers.Length == 0 || comparerMembers.Length == 0)
                continue;

            var typeFqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var converterFqn = converterMembers[0]
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var comparerFqn = comparerMembers[0]
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            results.Add(new VogenValueObjectRecord(typeFqn, converterFqn, comparerFqn));
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            FindVogenValueObjectsWithEfConverters(childNs, results);
        }
    }

    internal static bool IsVogenValueObject(INamedTypeSymbol typeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (
                attrClass is not null
                && attrClass.IsGenericType
                && attrClass.Name == "ValueObjectAttribute"
                && attrClass.ContainingNamespace.ToDisplayString() == "Vogen"
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// If the type is a Vogen value object, returns the FQN of its underlying primitive type.
    /// Otherwise returns the type's own FQN.
    /// </summary>
    internal static string ResolveUnderlyingType(ITypeSymbol typeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null)
                continue;

            // Vogen uses generic attribute ValueObjectAttribute<T>
            if (
                attrClass.IsGenericType
                && attrClass.Name == "ValueObjectAttribute"
                && attrClass.ContainingNamespace.ToDisplayString() == "Vogen"
                && attrClass.TypeArguments.Length == 1
            )
            {
                return attrClass
                    .TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
