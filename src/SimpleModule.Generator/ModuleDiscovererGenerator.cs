using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

[Generator]
public partial class ModuleDiscovererGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Extract an equatable data model from the compilation so the incremental
        // pipeline can cache results and skip re-generation when nothing changes.
        var dataProvider = context.CompilationProvider.Select(
            static (compilation, _) => ExtractDiscoveryData(compilation)
        );

        context.RegisterSourceOutput(
            dataProvider,
            static (spc, data) =>
            {
                if (data.Modules.Length == 0)
                    return;

                GenerateModuleExtensions(spc, data.Modules, data.DtoTypes.Length > 0);
                GenerateEndpointExtensions(spc, data.Modules);
                GenerateMenuExtensions(spc, data.Modules);
                GenerateRazorComponentExtensions(spc, data.Modules);
                GenerateViewPages(spc, data.Modules);

                if (data.DtoTypes.Length > 0)
                {
                    GenerateJsonResolver(spc, data.DtoTypes);
                    GenerateTypeScriptDefinitions(spc, data.DtoTypes);
                }
            }
        );
    }

    private static DiscoveryData ExtractDiscoveryData(Compilation compilation)
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
                            p.HasSetter
                        ))
                        .ToImmutableArray()
                ))
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
                                properties.Add(
                                    new DtoPropertyInfo
                                    {
                                        Name = prop.Name,
                                        TypeFqn = prop.Type.ToDisplayString(
                                            SymbolDisplayFormat.FullyQualifiedFormat
                                        ),
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
}
