using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class EndpointChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // SM0015: Duplicate view page name across modules
        var seenPages = new Dictionary<string, (string EndpointFqn, string ModuleName)>();
        foreach (var module in data.Modules)
        {
            foreach (var view in module.Views)
            {
                if (seenPages.TryGetValue(view.Page, out var existing))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.DuplicateViewPageName,
                            LocationHelper.ToLocation(view.Location),
                            view.Page,
                            Strip(existing.EndpointFqn),
                            existing.ModuleName,
                            Strip(view.FullyQualifiedName),
                            module.ModuleName
                        )
                    );
                }
                else
                {
                    seenPages[view.Page] = (view.FullyQualifiedName, module.ModuleName);
                }
            }
        }

        // SM0041: View page prefix must match module name
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
                continue;

            var expectedPrefix = module.ModuleName + "/";
            foreach (var view in module.Views)
            {
                if (!view.Page.StartsWith(expectedPrefix, System.StringComparison.Ordinal))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.ViewPagePrefixMismatch,
                            LocationHelper.ToLocation(view.Location),
                            Strip(view.FullyQualifiedName),
                            module.ModuleName,
                            view.Page
                        )
                    );
                }
            }
        }

        // SM0042: Module with views but no ViewPrefix
        foreach (var module in data.Modules)
        {
            if (module.Views.Length > 0 && string.IsNullOrEmpty(module.ViewPrefix))
            {
#pragma warning disable CA1308 // Route prefixes are conventionally lowercase
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ViewEndpointWithoutViewPrefix,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName,
                        module.Views.Length,
                        module.ModuleName.ToLowerInvariant()
                    )
                );
#pragma warning restore CA1308
            }
        }

        // SM0039: Interceptor depends on contract whose implementation takes a DbContext
        foreach (var interceptor in data.Interceptors)
        {
            foreach (var paramFqn in interceptor.ConstructorParamTypeFqns)
            {
                foreach (var impl in data.ContractImplementations)
                {
                    if (impl.InterfaceFqn == paramFqn && impl.DependsOnDbContext)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.InterceptorDependsOnDbContext,
                                LocationHelper.ToLocation(interceptor.Location),
                                Strip(interceptor.FullyQualifiedName),
                                interceptor.ModuleName,
                                Strip(paramFqn)
                            )
                        );
                    }
                }
            }
        }

        // SM0049: Multiple endpoints (IViewEndpoint) in a single file
        var viewsByFile = new Dictionary<string, List<(string Name, SourceLocationRecord Loc)>>();

        foreach (var module in data.Modules)
        {
            foreach (var view in module.Views)
            {
                if (view.Location is { } loc && !string.IsNullOrEmpty(loc.FilePath))
                {
                    if (!viewsByFile.TryGetValue(loc.FilePath, out var list))
                    {
                        list = new List<(string Name, SourceLocationRecord Loc)>();
                        viewsByFile[loc.FilePath] = list;
                    }

                    list.Add((Strip(view.FullyQualifiedName), loc));
                }
            }
        }

        foreach (var kvp in viewsByFile)
        {
            if (kvp.Value.Count > 1)
            {
                var names = string.Join(", ", kvp.Value.Select(e => e.Name));
                var fileName = Path.GetFileName(kvp.Key);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MultipleEndpointsPerFile,
                        LocationHelper.ToLocation(kvp.Value[1].Loc),
                        fileName,
                        names
                    )
                );
            }
        }

        // SM0052: Module assembly name must follow SimpleModule.{ModuleName} convention
        // SM0053: Module must have matching Contracts assembly
        // These checks only apply when the host project itself is a SimpleModule.* project.
        // User projects (e.g. TestApp.Host) use their own naming conventions.
        var hostIsFramework =
            data.HostAssemblyName?.StartsWith("SimpleModule.", System.StringComparison.Ordinal)
            == true;

        if (hostIsFramework)
        {
            var contractsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in data.ContractsAssemblyNames)
                contractsSet.Add(name);

            foreach (var module in data.Modules)
            {
                if (string.IsNullOrEmpty(module.ModuleName))
                    continue;

                // SM0052: Assembly naming convention
                // Accepted patterns: SimpleModule.{ModuleName} or SimpleModule.{ModuleName}.Module
                // The .Module suffix is allowed when a framework assembly with the same base name exists.
                var expectedAssemblyName = "SimpleModule." + module.ModuleName;
                var expectedModuleSuffix = expectedAssemblyName + ".Module";
                if (
                    !string.IsNullOrEmpty(module.AssemblyName)
                    && !string.Equals(
                        module.AssemblyName,
                        expectedAssemblyName,
                        System.StringComparison.Ordinal
                    )
                    && !string.Equals(
                        module.AssemblyName,
                        expectedModuleSuffix,
                        System.StringComparison.Ordinal
                    )
                    && !string.Equals(
                        module.AssemblyName,
                        data.HostAssemblyName,
                        System.StringComparison.Ordinal
                    )
                )
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.ModuleAssemblyNamingViolation,
                            LocationHelper.ToLocation(module.Location),
                            module.ModuleName,
                            module.AssemblyName
                        )
                    );
                }

                // SM0053: Missing contracts assembly
                var expectedContractsName = "SimpleModule." + module.ModuleName + ".Contracts";
                if (
                    !contractsSet.Contains(expectedContractsName)
                    && !string.Equals(
                        module.AssemblyName,
                        data.HostAssemblyName,
                        System.StringComparison.Ordinal
                    )
                )
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.MissingContractsAssembly,
                            LocationHelper.ToLocation(module.Location),
                            module.ModuleName,
                            module.AssemblyName
                        )
                    );
                }

                // SM0054: Endpoint missing Route const
                foreach (var endpoint in module.Endpoints)
                {
                    if (string.IsNullOrEmpty(endpoint.RouteTemplate))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.MissingEndpointRouteConst,
                                Location.None,
                                Strip(endpoint.FullyQualifiedName)
                            )
                        );
                    }
                }

                foreach (var view in module.Views)
                {
                    if (string.IsNullOrEmpty(view.RouteTemplate))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.MissingEndpointRouteConst,
                                LocationHelper.ToLocation(view.Location),
                                Strip(view.FullyQualifiedName)
                            )
                        );
                    }
                }
            }
        }
    }

    private static string Strip(string fqn) => TypeMappingHelpers.StripGlobalPrefix(fqn);
}
