using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PermissionFeatureFinder
{
    internal static void FindPermissionClasses(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol modulePermissionsSymbol,
        string moduleName,
        List<PermissionClassInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindPermissionClasses(childNs, modulePermissionsSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
                && SymbolHelpers.ImplementsInterface(typeSymbol, modulePermissionsSymbol)
            )
            {
                var info = new PermissionClassInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsSealed = typeSymbol.IsSealed,
                    Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                };

                // Collect public const string fields
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (
                        m is IFieldSymbol field
                        && field.DeclaredAccessibility == Accessibility.Public
                    )
                    {
                        info.Fields.Add(
                            new PermissionFieldInfo
                            {
                                FieldName = field.Name,
                                Value =
                                    field.HasConstantValue && field.ConstantValue is string s
                                        ? s
                                        : "",
                                IsConstString =
                                    field.IsConst
                                    && field.Type.SpecialType == SpecialType.System_String,
                                Location = SymbolHelpers.GetSourceLocation(field),
                            }
                        );
                    }
                }

                results.Add(info);
            }
        }
    }

    internal static void FindFeatureClasses(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleFeaturesSymbol,
        string moduleName,
        List<FeatureClassInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindFeatureClasses(childNs, moduleFeaturesSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
                && SymbolHelpers.ImplementsInterface(typeSymbol, moduleFeaturesSymbol)
            )
            {
                var info = new FeatureClassInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsSealed = typeSymbol.IsSealed,
                    Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                };

                // Collect public const string fields
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (
                        m is IFieldSymbol field
                        && field.DeclaredAccessibility == Accessibility.Public
                    )
                    {
                        info.Fields.Add(
                            new FeatureFieldInfo
                            {
                                FieldName = field.Name,
                                Value =
                                    field.HasConstantValue && field.ConstantValue is string s
                                        ? s
                                        : "",
                                IsConstString =
                                    field.IsConst
                                    && field.Type.SpecialType == SpecialType.System_String,
                                Location = SymbolHelpers.GetSourceLocation(field),
                            }
                        );
                    }
                }

                results.Add(info);
            }
        }
    }

    internal static void FindModuleOptionsClasses(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleOptionsSymbol,
        string moduleName,
        List<ModuleOptionsRecord> results
    )
    {
        SymbolHelpers.FindConcreteClassesImplementing(
            namespaceSymbol,
            moduleOptionsSymbol,
            typeSymbol =>
                results.Add(
                    new ModuleOptionsRecord(
                        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        moduleName,
                        SymbolHelpers.GetSourceLocation(typeSymbol)
                    )
                )
        );
    }
}
