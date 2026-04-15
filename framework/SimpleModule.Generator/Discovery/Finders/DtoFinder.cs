using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DtoFinder
{
    internal static void FindDtoTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol dtoAttributeSymbol,
        List<DtoTypeInfo> dtoTypes,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindDtoTypes(childNamespace, dtoAttributeSymbol, dtoTypes, cancellationToken);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            attr.AttributeClass,
                            dtoAttributeSymbol
                        )
                    )
                    {
                        var fqn = typeSymbol.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        );
                        var safeName = TypeMappingHelpers.StripGlobalPrefix(fqn).Replace(".", "_");

                        string? baseTypeFqn = null;
                        if (
                            typeSymbol.BaseType
                            is { SpecialType: not SpecialType.System_Object }
                                and var baseType
                        )
                        {
                            var baseFqn = baseType.ToDisplayString(
                                SymbolDisplayFormat.FullyQualifiedFormat
                            );
                            if (
                                baseType
                                    .GetAttributes()
                                    .Any(a =>
                                        SymbolEqualityComparer.Default.Equals(
                                            a.AttributeClass,
                                            dtoAttributeSymbol
                                        )
                                    )
                            )
                            {
                                baseTypeFqn = baseFqn;
                            }
                        }

                        dtoTypes.Add(
                            new DtoTypeInfo
                            {
                                FullyQualifiedName = fqn,
                                SafeName = safeName,
                                BaseTypeFqn = baseTypeFqn,
                                Properties = ExtractDtoProperties(typeSymbol),
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    internal static void FindConventionDtoTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? noDtoAttrSymbol,
        INamedTypeSymbol? eventInterfaceSymbol,
        HashSet<string> existingFqns,
        List<DtoTypeInfo> dtoTypes,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNs)
            {
                FindConventionDtoTypes(
                    childNs,
                    noDtoAttrSymbol,
                    eventInterfaceSymbol,
                    existingFqns,
                    dtoTypes,
                    cancellationToken
                );
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.DeclaredAccessibility == Accessibility.Public
                && !typeSymbol.IsStatic
                && typeSymbol.TypeKind != TypeKind.Interface
                && typeSymbol.TypeKind != TypeKind.Enum
                && typeSymbol.TypeKind != TypeKind.Delegate
            )
            {
                var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Skip if already found via [Dto]
                if (existingFqns.Contains(fqn))
                    continue;

                // Skip if [NoDtoGeneration]
                if (noDtoAttrSymbol is not null)
                {
                    var hasNoDtoAttr = false;
                    foreach (var attr in typeSymbol.GetAttributes())
                    {
                        if (
                            SymbolEqualityComparer.Default.Equals(
                                attr.AttributeClass,
                                noDtoAttrSymbol
                            )
                        )
                        {
                            hasNoDtoAttr = true;
                            break;
                        }
                    }
                    if (hasNoDtoAttr)
                        continue;
                }

                // Skip types that implement IEvent (events are not DTOs)
                if (eventInterfaceSymbol is not null)
                {
                    var isEvent = false;
                    foreach (var iface in typeSymbol.AllInterfaces)
                    {
                        if (SymbolEqualityComparer.Default.Equals(iface, eventInterfaceSymbol))
                        {
                            isEvent = true;
                            break;
                        }
                    }
                    if (isEvent)
                        continue;
                }

                // Skip generic type definitions (open generics like PagedResult<T>)
                if (typeSymbol.IsGenericType)
                    continue;

                // Skip Vogen-generated infrastructure types
                if (
                    typeSymbol.Name == "VogenTypesFactory"
                    || fqn.StartsWith("global::Vogen", StringComparison.Ordinal)
                )
                    continue;

                // Skip Vogen value objects — they have their own JsonConverter
                // and must not be treated as regular DTOs in the JSON resolver
                if (SymbolDiscovery.IsVogenValueObject(typeSymbol))
                    continue;

                var safeName = TypeMappingHelpers.StripGlobalPrefix(fqn).Replace(".", "_");

                string? baseTypeFqn = null;
                if (
                    typeSymbol.BaseType
                    is { SpecialType: not SpecialType.System_Object }
                        and var baseType
                )
                {
                    var baseFqn = baseType.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );
                    if (existingFqns.Contains(baseFqn))
                    {
                        baseTypeFqn = baseFqn;
                    }
                }

                existingFqns.Add(fqn);
                dtoTypes.Add(
                    new DtoTypeInfo
                    {
                        FullyQualifiedName = fqn,
                        SafeName = safeName,
                        BaseTypeFqn = baseTypeFqn,
                        Properties = ExtractDtoProperties(typeSymbol),
                    }
                );
            }
        }
    }

    private static List<DtoPropertyInfo> ExtractDtoProperties(INamedTypeSymbol typeSymbol)
    {
        // Walk the inheritance chain (most-derived first) so derived properties shadow
        // base properties of the same name. This lets DTOs inherit shared base classes
        // (e.g., AuditableEntity<TId> -> Id, CreatedAt, UpdatedAt, ConcurrencyStamp)
        // and still get serialized correctly.
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
                    var resolvedType = SymbolDiscovery.ResolveUnderlyingType(prop.Type);
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
