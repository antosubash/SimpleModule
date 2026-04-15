using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal sealed class DiagnosticEmitter : IEmitter
{
    public void Emit(SourceProductionContext context, DiscoveryData data)
    {
        ModuleChecks.Run(context, data);
        DbContextChecks.Run(context, data);
        DependencyChecks.Run(context, data);
        ContractAndDtoChecks.Run(context, data);

        // SM0004: DbContext with no DbSets — silently skipped.
        // Some DbContexts (e.g., OpenIddict) manage tables internally without public DbSet<T>
        // properties. These are excluded from the unified HostDbContext but are not an error.

        // SM0027/SM0031/SM0032/SM0033/SM0034: Permission diagnostics
        // Track permission values for duplicate detection (value -> class FQN)
        var permissionValueOwners = new Dictionary<string, string>();

        foreach (var perm in data.PermissionClasses)
        {
            var permCleanName = Strip(perm.FullyQualifiedName);

            // SM0032: Not sealed
            if (!perm.IsSealed)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.PermissionClassNotSealed,
                        LocationHelper.ToLocation(perm.Location),
                        permCleanName
                    )
                );
            }

            foreach (var field in perm.Fields)
            {
                // SM0027: Field is not const string
                if (!field.IsConstString)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.PermissionFieldNotConstString,
                            LocationHelper.ToLocation(field.Location),
                            permCleanName,
                            field.FieldName
                        )
                    );
                    continue;
                }

                // SM0031: Value doesn't match {Module}.{Action} pattern (exactly one dot)
                var dotCount = 0;
                foreach (var ch in field.Value)
                {
                    if (ch == '.')
                        dotCount++;
                }
                if (dotCount != 1)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.PermissionValueBadPattern,
                            LocationHelper.ToLocation(field.Location),
                            field.Value,
                            permCleanName
                        )
                    );
                }

                // SM0034: Value prefix doesn't match module name
                if (dotCount >= 1)
                {
                    var prefix = field.Value.Substring(0, field.Value.IndexOf('.'));
                    if (!string.Equals(prefix, perm.ModuleName, System.StringComparison.Ordinal))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.PermissionValueWrongPrefix,
                                LocationHelper.ToLocation(field.Location),
                                field.Value,
                                perm.ModuleName
                            )
                        );
                    }
                }

                // SM0033: Duplicate permission value
                if (permissionValueOwners.TryGetValue(field.Value, out var existingOwner))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.DuplicatePermissionValue,
                            LocationHelper.ToLocation(field.Location),
                            field.Value,
                            existingOwner,
                            permCleanName
                        )
                    );
                }
                else
                {
                    permissionValueOwners[field.Value] = permCleanName;
                }
            }
        }

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

        // SM0044: Multiple IModuleOptions for same module
        var optionsByModule = ModuleOptionsRecord.GroupByModule(data.ModuleOptions);

        foreach (var kvp in optionsByModule)
        {
            if (kvp.Value.Count > 1)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MultipleModuleOptions,
                        LocationHelper.ToLocation(kvp.Value[1].Location),
                        kvp.Key,
                        Strip(kvp.Value[0].FullyQualifiedName),
                        Strip(kvp.Value[1].FullyQualifiedName)
                    )
                );
            }
        }

        // SM0045/SM0046/SM0047/SM0048: Feature flag diagnostics
        var featureValueOwners = new Dictionary<string, string>();

        foreach (var feat in data.FeatureClasses)
        {
            var featCleanName = Strip(feat.FullyQualifiedName);

            // SM0045: Not sealed
            if (!feat.IsSealed)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.FeatureClassNotSealed,
                        LocationHelper.ToLocation(feat.Location),
                        featCleanName
                    )
                );
            }

            foreach (var field in feat.Fields)
            {
                // SM0048: Not a const string
                if (!field.IsConstString)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.FeatureFieldNotConstString,
                            LocationHelper.ToLocation(field.Location),
                            field.FieldName,
                            featCleanName
                        )
                    );
                    continue;
                }

                // SM0046: Naming violation
                if (!field.Value.Contains("."))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.FeatureFieldNamingViolation,
                            LocationHelper.ToLocation(field.Location),
                            field.Value,
                            featCleanName
                        )
                    );
                }

                // SM0047: Duplicate feature name
                if (featureValueOwners.TryGetValue(field.Value, out var existingOwner))
                {
                    if (existingOwner != feat.FullyQualifiedName)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.DuplicateFeatureName,
                                LocationHelper.ToLocation(field.Location),
                                field.Value,
                                Strip(existingOwner),
                                featCleanName
                            )
                        );
                    }
                }
                else
                {
                    featureValueOwners[field.Value] = feat.FullyQualifiedName;
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
