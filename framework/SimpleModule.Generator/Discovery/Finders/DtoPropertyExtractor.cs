using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DtoPropertyExtractor
{
    /// <summary>
    /// Walks the inheritance chain (most-derived first) so derived properties shadow
    /// base properties of the same name. This lets DTOs inherit shared base classes
    /// (e.g., AuditableEntity{TId} → Id, CreatedAt, UpdatedAt, ConcurrencyStamp)
    /// and still get serialized correctly.
    /// </summary>
    internal static List<DtoPropertyInfo> Extract(INamedTypeSymbol typeSymbol)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var properties = new List<DtoPropertyInfo>();
        for (
            var current = typeSymbol;
            current is not null && current.SpecialType != SpecialType.System_Object;
            current = current.BaseType
        )
        {
            foreach (var m in current.GetMembers())
            {
                if (
                    m is IPropertySymbol prop
                    && prop.DeclaredAccessibility == Accessibility.Public
                    && !prop.IsStatic
                    && !prop.IsIndexer
                    && prop.GetMethod is not null
                    && !HasJsonIgnoreAttribute(prop)
                    && seen.Add(prop.Name)
                )
                {
                    var resolvedType = VogenFinder.ResolveUnderlyingType(prop.Type);
                    var actualType = prop.Type.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );
                    properties.Add(
                        new DtoPropertyInfo
                        {
                            Name = prop.Name,
                            TypeFqn = actualType,
                            UnderlyingTypeFqn = resolvedType != actualType ? resolvedType : null,
                            HasSetter =
                                prop.SetMethod is not null
                                && prop.SetMethod.DeclaredAccessibility == Accessibility.Public,
                        }
                    );
                }
            }
        }
        return properties;
    }

    /// <summary>
    /// Returns true if the property is decorated with <c>[System.Text.Json.Serialization.JsonIgnore]</c>.
    /// Properties marked this way are excluded from generated JSON metadata, mirroring
    /// runtime System.Text.Json behavior.
    /// </summary>
    private static bool HasJsonIgnoreAttribute(IPropertySymbol prop)
    {
        foreach (var attr in prop.GetAttributes())
        {
            var name = attr.AttributeClass?.ToDisplayString();
            if (name == "System.Text.Json.Serialization.JsonIgnoreAttribute")
            {
                return true;
            }
        }
        return false;
    }
}
