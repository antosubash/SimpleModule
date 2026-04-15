using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class ModuleFinder
{
    internal static void FindModuleTypes(
        INamespaceSymbol namespaceSymbol,
        CoreSymbols symbols,
        List<ModuleInfo> modules,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindModuleTypes(childNamespace, symbols, modules, cancellationToken);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            attr.AttributeClass,
                            symbols.ModuleAttribute
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
                                HasConfigureServices =
                                    SymbolHelpers.DeclaresMethod(typeSymbol, "ConfigureServices")
                                    || (
                                        symbols.ModuleServices is not null
                                        && SymbolHelpers.ImplementsInterface(
                                            typeSymbol,
                                            symbols.ModuleServices
                                        )
                                    ),
                                HasConfigureEndpoints = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureEndpoints"
                                ),
                                HasConfigureMenu =
                                    SymbolHelpers.DeclaresMethod(typeSymbol, "ConfigureMenu")
                                    || (
                                        symbols.ModuleMenu is not null
                                        && SymbolHelpers.ImplementsInterface(
                                            typeSymbol,
                                            symbols.ModuleMenu
                                        )
                                    ),
                                HasConfigureMiddleware =
                                    SymbolHelpers.DeclaresMethod(typeSymbol, "ConfigureMiddleware")
                                    || (
                                        symbols.ModuleMiddleware is not null
                                        && SymbolHelpers.ImplementsInterface(
                                            typeSymbol,
                                            symbols.ModuleMiddleware
                                        )
                                    ),
                                HasConfigurePermissions = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigurePermissions"
                                ),
                                HasConfigureSettings =
                                    SymbolHelpers.DeclaresMethod(typeSymbol, "ConfigureSettings")
                                    || (
                                        symbols.ModuleSettings is not null
                                        && SymbolHelpers.ImplementsInterface(
                                            typeSymbol,
                                            symbols.ModuleSettings
                                        )
                                    ),
                                HasConfigureFeatureFlags = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureFeatureFlags"
                                ),
                                HasConfigureAgents = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureAgents"
                                ),
                                HasConfigureRateLimits = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureRateLimits"
                                ),
                                RoutePrefix = routePrefix,
                                ViewPrefix = viewPrefix,
                                AssemblyName = typeSymbol.ContainingAssembly.Name,
                                Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                            }
                        );
                        break;
                    }
                }
            }
        }
    }
}
