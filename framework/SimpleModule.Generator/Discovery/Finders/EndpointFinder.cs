using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class EndpointFinder
{
    internal static void FindEndpointTypes(
        INamespaceSymbol namespaceSymbol,
        CoreSymbols symbols,
        List<EndpointInfo> endpoints,
        List<ViewInfo> views,
        CancellationToken cancellationToken
    )
    {
        if (symbols.EndpointInterface is null)
            return;

        FindEndpointTypesInternal(
            namespaceSymbol,
            symbols.EndpointInterface,
            symbols.ViewEndpointInterface,
            endpoints,
            views,
            cancellationToken
        );
    }

    private static void FindEndpointTypesInternal(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol endpointInterfaceSymbol,
        INamedTypeSymbol? viewEndpointInterfaceSymbol,
        List<EndpointInfo> endpoints,
        List<ViewInfo> views,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindEndpointTypesInternal(
                    childNamespace,
                    endpointInterfaceSymbol,
                    viewEndpointInterfaceSymbol,
                    endpoints,
                    views,
                    cancellationToken
                );
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                if (!typeSymbol.IsAbstract && !typeSymbol.IsStatic)
                {
                    var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if (
                        viewEndpointInterfaceSymbol is not null
                        && SymbolHelpers.ImplementsInterface(
                            typeSymbol,
                            viewEndpointInterfaceSymbol
                        )
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

                        var viewInfo = new ViewInfo
                        {
                            FullyQualifiedName = fqn,
                            InferredClassName = className,
                            Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                        };

                        var (viewRoute, _) = ReadRouteConstFields(typeSymbol);
                        viewInfo.RouteTemplate = viewRoute;
                        views.Add(viewInfo);
                    }
                    else if (SymbolHelpers.ImplementsInterface(typeSymbol, endpointInterfaceSymbol))
                    {
                        var info = new EndpointInfo { FullyQualifiedName = fqn };

                        foreach (var attr in typeSymbol.GetAttributes())
                        {
                            var attrName = attr.AttributeClass?.ToDisplayString(
                                SymbolDisplayFormat.FullyQualifiedFormat
                            );

                            if (
                                attrName
                                == "global::SimpleModule.Core.Authorization.RequirePermissionAttribute"
                            )
                            {
                                if (attr.ConstructorArguments.Length > 0)
                                {
                                    var arg = attr.ConstructorArguments[0];
                                    if (arg.Kind == TypedConstantKind.Array)
                                    {
                                        foreach (var val in arg.Values)
                                        {
                                            if (val.Value is string s)
                                                info.RequiredPermissions.Add(s);
                                        }
                                    }
                                    else if (arg.Value is string single)
                                    {
                                        info.RequiredPermissions.Add(single);
                                    }
                                }
                            }
                            else if (
                                attrName
                                == "global::Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute"
                            )
                            {
                                info.AllowAnonymous = true;
                            }
                        }

                        var (epRoute, epMethod) = ReadRouteConstFields(typeSymbol);
                        info.RouteTemplate = epRoute;
                        info.HttpMethod = epMethod;
                        endpoints.Add(info);
                    }
                }
            }
        }
    }

    private static (string route, string method) ReadRouteConstFields(INamedTypeSymbol typeSymbol)
    {
        var route = "";
        var method = "";
        foreach (var m in typeSymbol.GetMembers())
        {
            if (m is IFieldSymbol { IsConst: true, ConstantValue: string value } field)
            {
                if (field.Name == "Route")
                    route = value;
                else if (field.Name == "Method")
                    method = value;
            }
        }
        return (route, method);
    }

    /// <summary>
    /// For each module, scans the module's own implementation assembly (once per
    /// unique assembly) and distributes every discovered endpoint/view to the
    /// module whose namespace is closest. Views also get their page name inferred
    /// from namespace segments (e.g. SimpleModule.Users.Pages.Account.LoginEndpoint
    /// becomes Users/Account/Login).
    /// </summary>
    internal static void Discover(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        CoreSymbols symbols,
        CancellationToken cancellationToken
    )
    {
        var endpointScannedAssemblies = new HashSet<IAssemblySymbol>(
            SymbolEqualityComparer.Default
        );
        foreach (var module in modules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var assembly = typeSymbol.ContainingAssembly;
            if (!endpointScannedAssemblies.Add(assembly))
                continue;

            var rawEndpoints = new List<EndpointInfo>();
            var rawViews = new List<ViewInfo>();
            FindEndpointTypes(
                assembly.GlobalNamespace,
                symbols,
                rawEndpoints,
                rawViews,
                cancellationToken
            );

            // Match each endpoint/view to the module whose namespace is closest
            foreach (var ep in rawEndpoints)
            {
                var epFqn = TypeMappingHelpers.StripGlobalPrefix(ep.FullyQualifiedName);
                var ownerName = SymbolHelpers.FindClosestModuleName(epFqn, modules);
                var owner = modules.Find(m => m.ModuleName == ownerName);
                if (owner is not null)
                    owner.Endpoints.Add(ep);
            }

            // Pre-compute module namespace per module name for page inference
            var moduleNsByName = new Dictionary<string, string>();
            foreach (var m in modules)
            {
                if (!moduleNsByName.ContainsKey(m.ModuleName))
                {
                    var mFqn = TypeMappingHelpers.StripGlobalPrefix(m.FullyQualifiedName);
                    moduleNsByName[m.ModuleName] = TypeMappingHelpers.ExtractNamespace(mFqn);
                }
            }

            foreach (var v in rawViews)
            {
                var vFqn = TypeMappingHelpers.StripGlobalPrefix(v.FullyQualifiedName);
                var ownerName = SymbolHelpers.FindClosestModuleName(vFqn, modules);
                var owner = modules.Find(m => m.ModuleName == ownerName);
                if (owner is not null)
                {
                    // Derive page name from namespace segments between module NS and class name.
                    // e.g. SimpleModule.Users.Pages.Account.LoginEndpoint → Users/Account/Login
                    if (v.Page is null)
                    {
                        var moduleNs = moduleNsByName[ownerName];
                        var typeNs = TypeMappingHelpers.ExtractNamespace(vFqn);

                        // Extract segments after the module namespace, stripping Views/Pages
                        var remaining =
                            typeNs.Length > moduleNs.Length
                                ? typeNs.Substring(moduleNs.Length).TrimStart('.')
                                : "";

                        var segments = remaining.Split('.');
                        var pathParts = new List<string>();
                        foreach (var seg in segments)
                        {
                            if (
                                seg.Length > 0
                                && !seg.Equals("Views", StringComparison.Ordinal)
                                && !seg.Equals("Pages", StringComparison.Ordinal)
                            )
                            {
                                pathParts.Add(seg);
                            }
                        }

                        var subPath = pathParts.Count > 0 ? string.Join("/", pathParts) + "/" : "";
                        v.Page = ownerName + "/" + subPath + v.InferredClassName;
                    }

                    owner.Views.Add(v);
                }
            }
        }
    }
}
