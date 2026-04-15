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
}
