using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class SymbolDiscovery
{
    internal static DiscoveryData Extract(Compilation compilation)
    {
        var moduleAttributeSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.ModuleAttribute"
        );
        if (moduleAttributeSymbol is null)
            return DiscoveryData.Empty;

        var dtoAttributeSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.DtoAttribute"
        );

        var endpointInterfaceSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.IEndpoint"
        );

        var viewEndpointInterfaceSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.IViewEndpoint"
        );

        var modules = new List<ModuleInfo>();

        foreach (var reference in compilation.References)
        {
            if (
                compilation.GetAssemblyOrModuleSymbol(reference)
                is not IAssemblySymbol assemblySymbol
            )
                continue;

            FindModuleTypes(assemblySymbol.GlobalNamespace, moduleAttributeSymbol, modules);
        }

        FindModuleTypes(compilation.Assembly.GlobalNamespace, moduleAttributeSymbol, modules);

        if (modules.Count == 0)
            return DiscoveryData.Empty;

        // Discover IEndpoint implementors per module assembly.
        // Classification is by interface type: IViewEndpoint -> view, IEndpoint -> API.
        if (endpointInterfaceSymbol is not null)
        {
            foreach (var module in modules)
            {
                var metadataName = module.FullyQualifiedName.Replace("global::", "");
                var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
                if (typeSymbol is null)
                    continue;

                var assembly = typeSymbol.ContainingAssembly;
                FindEndpointTypes(
                    assembly.GlobalNamespace,
                    endpointInterfaceSymbol,
                    viewEndpointInterfaceSymbol,
                    module.ModuleName,
                    module.Endpoints,
                    module.Views
                );
            }
        }

        // Discover DbContext subclasses and IEntityTypeConfiguration<T> per module assembly.
        // Scan each assembly once, then match DbContexts/configs to the nearest module by namespace.
        var dbContexts = new List<DbContextInfo>();
        var entityConfigs = new List<EntityConfigInfo>();
        var scannedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var module in modules)
        {
            var metadataName = module.FullyQualifiedName.Replace("global::", "");
            var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
            if (typeSymbol is null)
                continue;

            var assembly = typeSymbol.ContainingAssembly;
            if (!scannedAssemblies.Add(assembly))
                continue;

            // Collect unmatched items from this assembly
            var rawDbContexts = new List<DbContextInfo>();
            var rawEntityConfigs = new List<EntityConfigInfo>();
            FindDbContextTypes(assembly.GlobalNamespace, "", rawDbContexts);
            FindEntityConfigTypes(assembly.GlobalNamespace, "", rawEntityConfigs);

            // Match each DbContext to the module whose namespace is closest
            foreach (var ctx in rawDbContexts)
            {
                var ctxNs = ctx.FullyQualifiedName.Replace("global::", "");
                ctx.ModuleName = FindClosestModuleName(ctxNs, modules);
                dbContexts.Add(ctx);
            }

            foreach (var cfg in rawEntityConfigs)
            {
                var cfgNs = cfg.ConfigFqn.Replace("global::", "");
                cfg.ModuleName = FindClosestModuleName(cfgNs, modules);
                entityConfigs.Add(cfg);
            }
        }

        var dtoTypes = new List<DtoTypeInfo>();
        if (dtoAttributeSymbol is not null)
        {
            foreach (var reference in compilation.References)
            {
                if (
                    compilation.GetAssemblyOrModuleSymbol(reference)
                    is not IAssemblySymbol assemblySymbol
                )
                    continue;

                FindDtoTypes(assemblySymbol.GlobalNamespace, dtoAttributeSymbol, dtoTypes);
            }

            FindDtoTypes(compilation.Assembly.GlobalNamespace, dtoAttributeSymbol, dtoTypes);
        }

        var componentBaseSymbol = compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Components.ComponentBase"
        );
        if (componentBaseSymbol is not null)
        {
            var assembliesWithComponents = new HashSet<IAssemblySymbol>(
                SymbolEqualityComparer.Default
            );
            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol asm)
                    continue;

                if (HasComponentBaseDescendant(asm.GlobalNamespace, componentBaseSymbol))
                    assembliesWithComponents.Add(asm);
            }

            foreach (var module in modules)
            {
                var metadataName = module.FullyQualifiedName.Replace("global::", "");
                var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
                if (
                    typeSymbol is not null
                    && assembliesWithComponents.Contains(typeSymbol.ContainingAssembly)
                )
                    module.HasRazorComponents = true;
            }
        }

        return new DiscoveryData(
            modules
                .Select(m => new ModuleInfoRecord(
                    m.FullyQualifiedName,
                    m.ModuleName,
                    m.HasConfigureServices,
                    m.HasConfigureEndpoints,
                    m.HasConfigureMenu,
                    m.HasRazorComponents,
                    m.RoutePrefix,
                    m.ViewPrefix,
                    m.Endpoints.Select(e => new EndpointInfoRecord(e.FullyQualifiedName))
                        .ToImmutableArray(),
                    m.Views.Select(v => new ViewInfoRecord(v.FullyQualifiedName, v.Page))
                        .ToImmutableArray()
                ))
                .ToImmutableArray(),
            dtoTypes
                .Select(d => new DtoTypeInfoRecord(
                    d.FullyQualifiedName,
                    d.SafeName,
                    d.Properties.Select(p => new DtoPropertyInfoRecord(
                            p.Name,
                            p.TypeFqn,
                            p.UnderlyingTypeFqn,
                            p.HasSetter
                        ))
                        .ToImmutableArray()
                ))
                .ToImmutableArray(),
            dbContexts
                .Select(c => new DbContextInfoRecord(
                    c.FullyQualifiedName,
                    c.ModuleName,
                    c.IsIdentityDbContext,
                    c.IdentityUserTypeFqn,
                    c.IdentityRoleTypeFqn,
                    c.IdentityKeyTypeFqn,
                    c.DbSets.Select(d => new DbSetInfoRecord(d.PropertyName, d.EntityFqn))
                        .ToImmutableArray()
                ))
                .ToImmutableArray(),
            entityConfigs
                .Select(e => new EntityConfigInfoRecord(e.ConfigFqn, e.EntityFqn, e.ModuleName))
                .ToImmutableArray()
        );
    }

    private static void FindModuleTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleAttributeSymbol,
        List<ModuleInfo> modules
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindModuleTypes(childNamespace, moduleAttributeSymbol, modules);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            attr.AttributeClass,
                            moduleAttributeSymbol
                        )
                    )
                    {
                        var moduleName =
                            attr.ConstructorArguments.Length > 0
                                ? attr.ConstructorArguments[0].Value as string ?? ""
                                : "";
                        var routePrefix = "";
                        var viewPrefix = "";
                        foreach (var namedArg in attr.NamedArguments)
                        {
                            if (
                                namedArg.Key == "RoutePrefix"
                                && namedArg.Value.Value is string prefix
                            )
                            {
                                routePrefix = prefix;
                            }
                            else if (
                                namedArg.Key == "ViewPrefix"
                                && namedArg.Value.Value is string vPrefix
                            )
                            {
                                viewPrefix = vPrefix;
                            }
                        }

                        modules.Add(
                            new ModuleInfo
                            {
                                FullyQualifiedName = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                ModuleName = moduleName,
                                HasConfigureServices = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureServices"
                                ),
                                HasConfigureEndpoints = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureEndpoints"
                                ),
                                HasConfigureMenu = DeclaresMethod(typeSymbol, "ConfigureMenu"),
                                RoutePrefix = routePrefix,
                                ViewPrefix = viewPrefix,
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    private static void FindEndpointTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol endpointInterfaceSymbol,
        INamedTypeSymbol? viewEndpointInterfaceSymbol,
        string moduleName,
        List<EndpointInfo> endpoints,
        List<ViewInfo> views
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindEndpointTypes(
                    childNamespace,
                    endpointInterfaceSymbol,
                    viewEndpointInterfaceSymbol,
                    moduleName,
                    endpoints,
                    views
                );
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                if (!typeSymbol.IsAbstract && !typeSymbol.IsStatic)
                {
                    var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if (
                        viewEndpointInterfaceSymbol is not null
                        && ImplementsInterface(typeSymbol, viewEndpointInterfaceSymbol)
                    )
                    {
                        var className = typeSymbol.Name;
                        if (className.EndsWith("Endpoint", StringComparison.Ordinal))
                            className = className.Substring(
                                0,
                                className.Length - "Endpoint".Length
                            );
                        else if (className.EndsWith("View", StringComparison.Ordinal))
                            className = className.Substring(0, className.Length - "View".Length);

                        views.Add(
                            new ViewInfo
                            {
                                FullyQualifiedName = fqn,
                                Page = moduleName + "/" + className,
                            }
                        );
                    }
                    else if (ImplementsInterface(typeSymbol, endpointInterfaceSymbol))
                    {
                        endpoints.Add(new EndpointInfo { FullyQualifiedName = fqn });
                    }
                }
            }
        }
    }

    private static bool ImplementsInterface(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol interfaceSymbol
    )
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, interfaceSymbol))
                return true;
        }
        return false;
    }

    private static bool DeclaresMethod(INamedTypeSymbol typeSymbol, string methodName)
    {
        foreach (var member in typeSymbol.GetMembers(methodName))
        {
            if (member is IMethodSymbol)
                return true;
        }
        return false;
    }

    private static void FindDtoTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol dtoAttributeSymbol,
        List<DtoTypeInfo> dtoTypes
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindDtoTypes(childNamespace, dtoAttributeSymbol, dtoTypes);
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
                        var safeName = fqn.Replace("global::", "").Replace(".", "_");

                        var properties = new List<DtoPropertyInfo>();
                        foreach (var m in typeSymbol.GetMembers())
                        {
                            if (
                                m is IPropertySymbol prop
                                && prop.DeclaredAccessibility == Accessibility.Public
                                && !prop.IsStatic
                                && !prop.IsIndexer
                                && prop.GetMethod is not null
                            )
                            {
                                var resolvedType = ResolveUnderlyingType(prop.Type);
                                var actualType = prop.Type.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                );
                                properties.Add(
                                    new DtoPropertyInfo
                                    {
                                        Name = prop.Name,
                                        TypeFqn = actualType,
                                        UnderlyingTypeFqn = resolvedType != actualType
                                            ? resolvedType
                                            : null,
                                        HasSetter =
                                            prop.SetMethod is not null
                                            && prop.SetMethod.DeclaredAccessibility
                                                == Accessibility.Public,
                                    }
                                );
                            }
                        }

                        dtoTypes.Add(
                            new DtoTypeInfo
                            {
                                FullyQualifiedName = fqn,
                                SafeName = safeName,
                                Properties = properties,
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    private static bool HasComponentBaseDescendant(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol componentBaseSymbol
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                if (HasComponentBaseDescendant(childNamespace, componentBaseSymbol))
                    return true;
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                if (InheritsFrom(typeSymbol, componentBaseSymbol))
                    return true;
            }
        }
        return false;
    }

    private static bool InheritsFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseType)
    {
        var current = typeSymbol.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static string FindClosestModuleName(string typeFqn, List<ModuleInfo> modules)
    {
        // Match by longest shared namespace prefix between the type and each module class.
        var bestMatch = "";
        var bestLength = -1;
        foreach (var module in modules)
        {
            var moduleFqn = module.FullyQualifiedName.Replace("global::", "");
            var moduleNs = moduleFqn.Contains(".")
                ? moduleFqn.Substring(0, moduleFqn.LastIndexOf('.'))
                : "";

            if (
                typeFqn.StartsWith(moduleNs, StringComparison.Ordinal)
                && moduleNs.Length > bestLength
            )
            {
                bestLength = moduleNs.Length;
                bestMatch = module.ModuleName;
            }
        }

        return bestMatch.Length > 0 ? bestMatch : modules[0].ModuleName;
    }

    private static void FindDbContextTypes(
        INamespaceSymbol namespaceSymbol,
        string moduleName,
        List<DbContextInfo> dbContexts
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindDbContextTypes(childNamespace, moduleName, dbContexts);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
            )
            {
                // Walk base type chain looking for DbContext
                var isDbContext = false;
                var isIdentity = false;
                string identityUserFqn = "";
                string identityRoleFqn = "";
                string identityKeyFqn = "";

                var current = typeSymbol.BaseType;
                while (current is not null)
                {
                    var baseFqn = current.OriginalDefinition.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );

                    if (
                        baseFqn
                        == "global::Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<TUser, TRole, TKey>"
                    )
                    {
                        isDbContext = true;
                        isIdentity = true;
                        if (current.TypeArguments.Length >= 3)
                        {
                            identityUserFqn = current
                                .TypeArguments[0]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            identityRoleFqn = current
                                .TypeArguments[1]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            identityKeyFqn = current
                                .TypeArguments[2]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        }
                        break;
                    }

                    if (baseFqn == "global::Microsoft.EntityFrameworkCore.DbContext")
                    {
                        isDbContext = true;
                        break;
                    }

                    current = current.BaseType;
                }

                if (!isDbContext)
                    continue;

                var info = new DbContextInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsIdentityDbContext = isIdentity,
                    IdentityUserTypeFqn = identityUserFqn,
                    IdentityRoleTypeFqn = identityRoleFqn,
                    IdentityKeyTypeFqn = identityKeyFqn,
                };

                // Collect DbSet<T> properties
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (
                        m is IPropertySymbol prop
                        && prop.DeclaredAccessibility == Accessibility.Public
                        && !prop.IsStatic
                        && prop.Type is INamedTypeSymbol propType
                        && propType.IsGenericType
                        && propType.OriginalDefinition.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ) == "global::Microsoft.EntityFrameworkCore.DbSet<TEntity>"
                    )
                    {
                        var entityFqn = propType
                            .TypeArguments[0]
                            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        info.DbSets.Add(
                            new DbSetInfo { PropertyName = prop.Name, EntityFqn = entityFqn }
                        );
                    }
                }

                dbContexts.Add(info);
            }
        }
    }

    private static void FindEntityConfigTypes(
        INamespaceSymbol namespaceSymbol,
        string moduleName,
        List<EntityConfigInfo> entityConfigs
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindEntityConfigTypes(childNamespace, moduleName, entityConfigs);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
            )
            {
                foreach (var iface in typeSymbol.AllInterfaces)
                {
                    if (
                        iface.IsGenericType
                        && iface.OriginalDefinition.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        )
                            == "global::Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<TEntity>"
                    )
                    {
                        var entityFqn = iface
                            .TypeArguments[0]
                            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        entityConfigs.Add(
                            new EntityConfigInfo
                            {
                                ConfigFqn = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                EntityFqn = entityFqn,
                                ModuleName = moduleName,
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// If the type is a Vogen value object, returns the FQN of its underlying primitive type.
    /// Otherwise returns the type's own FQN.
    /// </summary>
    private static string ResolveUnderlyingType(ITypeSymbol typeSymbol)
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
                return attrClass.TypeArguments[0].ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );
            }
        }

        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
