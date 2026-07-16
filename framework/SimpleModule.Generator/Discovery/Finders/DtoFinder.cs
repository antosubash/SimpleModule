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
                                HasParameterlessConstructor = HasPublicParameterlessConstructor(
                                    typeSymbol
                                ),
                                Properties = DtoPropertyExtractor.Extract(typeSymbol),
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
                // Skip walking into System.*, Microsoft.*, or Vogen.* trees — they never contain
                // convention DTOs and traversing them adds zero value while inflating symbol-tree walks.
                var childName = childNs.Name;
                if (childName == "System" || childName == "Microsoft" || childName == "Vogen")
                    continue;

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
                if (VogenFinder.IsVogenValueObject(typeSymbol))
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
                        HasParameterlessConstructor = HasPublicParameterlessConstructor(
                            typeSymbol
                        ),
                        Properties = DtoPropertyExtractor.Extract(typeSymbol),
                    }
                );
            }
        }
    }

    /// <summary>
    /// True when <c>new T()</c> is valid for the type: non-abstract with a public
    /// parameterless instance constructor. Positional records and ctor-only types
    /// return false so the JSON resolver skips <c>CreateObject</c> for them.
    /// </summary>
    private static bool HasPublicParameterlessConstructor(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.IsAbstract)
            return false;

        foreach (var ctor in typeSymbol.InstanceConstructors)
        {
            if (ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Scans every referenced assembly AND the host assembly for types decorated
    /// with <c>[Dto]</c>. No-op when the DtoAttribute symbol isn't resolvable.
    /// </summary>
    internal static void DiscoverAttributedDtos(
        IReadOnlyList<IAssemblySymbol> refAssemblies,
        INamespaceSymbol hostGlobalNamespace,
        CoreSymbols symbols,
        List<DtoTypeInfo> dtoTypes,
        CancellationToken cancellationToken
    )
    {
        if (symbols.DtoAttribute is null)
            return;

        foreach (var assemblySymbol in refAssemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FindDtoTypes(
                assemblySymbol.GlobalNamespace,
                symbols.DtoAttribute,
                dtoTypes,
                cancellationToken
            );
        }

        FindDtoTypes(hostGlobalNamespace, symbols.DtoAttribute, dtoTypes, cancellationToken);

        // [FormRequest] types also get TypeScript interfaces (same path as [Dto])
        if (symbols.FormRequestAttribute is null)
            return;

        // Build a set of already-discovered FQNs so types with both [Dto] and
        // [FormRequest] are not added twice (which would cause duplicate TS exports).
        var existingFqns = new HashSet<string>();
        foreach (var d in dtoTypes)
            existingFqns.Add(d.FullyQualifiedName);

        var formRequestDtos = new List<DtoTypeInfo>();
        foreach (var assemblySymbol in refAssemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FindDtoTypes(
                assemblySymbol.GlobalNamespace,
                symbols.FormRequestAttribute,
                formRequestDtos,
                cancellationToken
            );
        }

        FindDtoTypes(
            hostGlobalNamespace,
            symbols.FormRequestAttribute,
            formRequestDtos,
            cancellationToken
        );

        foreach (var dto in formRequestDtos)
        {
            if (!existingFqns.Contains(dto.FullyQualifiedName))
                dtoTypes.Add(dto);
        }
    }
}
