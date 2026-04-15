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

    /// <summary>
    /// Scans every module's implementation assembly AND every contracts assembly
    /// for IModulePermissions implementors. No-op when ModulePermissions isn't
    /// resolvable in the compilation.
    /// </summary>
    internal static void DiscoverPermissions(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Dictionary<string, IAssemblySymbol> contractsAssemblySymbols,
        Dictionary<string, string> contractsAssemblyMap,
        CoreSymbols symbols,
        List<PermissionClassInfo> permissionClasses
    )
    {
        if (symbols.ModulePermissions is not null)
        {
            foreach (var module in modules)
            {
                if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                    continue;

                var moduleAssembly = typeSymbol.ContainingAssembly;
                FindPermissionClasses(
                    moduleAssembly.GlobalNamespace,
                    symbols.ModulePermissions,
                    module.ModuleName,
                    permissionClasses
                );
            }

            // Also scan contracts assemblies for permission classes
            foreach (var kvp in contractsAssemblySymbols)
            {
                if (contractsAssemblyMap.TryGetValue(kvp.Key, out var moduleName))
                {
                    FindPermissionClasses(
                        kvp.Value.GlobalNamespace,
                        symbols.ModulePermissions,
                        moduleName,
                        permissionClasses
                    );
                }
            }
        }
    }

    /// <summary>
    /// Scans every module's implementation assembly AND every contracts assembly
    /// for IModuleFeatures implementors. No-op when ModuleFeatures isn't
    /// resolvable in the compilation.
    /// </summary>
    internal static void DiscoverFeatures(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Dictionary<string, IAssemblySymbol> contractsAssemblySymbols,
        Dictionary<string, string> contractsAssemblyMap,
        CoreSymbols symbols,
        List<FeatureClassInfo> featureClasses
    )
    {
        if (symbols.ModuleFeatures is not null)
        {
            foreach (var module in modules)
            {
                if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                    continue;

                var moduleAssembly = typeSymbol.ContainingAssembly;
                FindFeatureClasses(
                    moduleAssembly.GlobalNamespace,
                    symbols.ModuleFeatures,
                    module.ModuleName,
                    featureClasses
                );
            }

            // Also scan contracts assemblies for feature classes
            foreach (var kvp in contractsAssemblySymbols)
            {
                if (contractsAssemblyMap.TryGetValue(kvp.Key, out var moduleName))
                {
                    FindFeatureClasses(
                        kvp.Value.GlobalNamespace,
                        symbols.ModuleFeatures,
                        moduleName,
                        featureClasses
                    );
                }
            }
        }
    }

    /// <summary>
    /// Scans every module's implementation assembly AND every contracts assembly
    /// for IModuleOptions implementors. No-op when ModuleOptions isn't
    /// resolvable in the compilation.
    /// </summary>
    internal static void DiscoverModuleOptions(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Dictionary<string, IAssemblySymbol> contractsAssemblySymbols,
        Dictionary<string, string> contractsAssemblyMap,
        CoreSymbols symbols,
        List<ModuleOptionsRecord> moduleOptionsList
    )
    {
        if (symbols.ModuleOptions is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindModuleOptionsClasses(
                        assembly.GlobalNamespace,
                        symbols.ModuleOptions,
                        module.ModuleName,
                        moduleOptionsList
                    )
            );

            // Also scan contracts assemblies for module options classes
            foreach (var kvp in contractsAssemblySymbols)
            {
                if (contractsAssemblyMap.TryGetValue(kvp.Key, out var moduleName))
                {
                    FindModuleOptionsClasses(
                        kvp.Value.GlobalNamespace,
                        symbols.ModuleOptions,
                        moduleName,
                        moduleOptionsList
                    );
                }
            }
        }
    }
}
