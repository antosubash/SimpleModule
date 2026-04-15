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
        // SM0002: Empty module name
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EmptyModuleName,
                        LocationHelper.ToLocation(module.Location),
                        Strip(module.FullyQualifiedName)
                    )
                );
            }
        }

        // SM0040: Duplicate module name
        var seenModuleNames = new Dictionary<string, string>();
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
                continue;

            if (seenModuleNames.TryGetValue(module.ModuleName, out var existingFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateModuleName,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName,
                        Strip(existingFqn),
                        Strip(module.FullyQualifiedName)
                    )
                );
            }
            else
            {
                seenModuleNames[module.ModuleName] = module.FullyQualifiedName;
            }
        }

        // SM0043: Empty module (no IModule methods overridden)
        var moduleNamesWithDbContext = new HashSet<string>(
            data.DbContexts.Select(db => db.ModuleName),
            StringComparer.Ordinal
        );
        foreach (var module in data.Modules)
        {
            if (
                !module.HasConfigureServices
                && !module.HasConfigureEndpoints
                && !module.HasConfigureMenu
                && !module.HasConfigurePermissions
                && !module.HasConfigureMiddleware
                && !module.HasConfigureSettings
                && !module.HasConfigureFeatureFlags
                && module.Endpoints.Length == 0
                && module.Views.Length == 0
                && !moduleNamesWithDbContext.Contains(module.ModuleName)
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EmptyModuleWarning,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName
                    )
                );
            }
        }

        // SM0004: DbContext with no DbSets — silently skipped.
        // Some DbContexts (e.g., OpenIddict) manage tables internally without public DbSet<T>
        // properties. These are excluded from the unified HostDbContext but are not an error.

        // SM0003: Multiple IdentityDbContexts
        DbContextInfoRecord? firstIdentity = null;
        foreach (var ctx in data.DbContexts)
        {
            if (!ctx.IsIdentityDbContext)
                continue;

            if (firstIdentity is null)
            {
                firstIdentity = ctx;
            }
            else
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MultipleIdentityDbContexts,
                        LocationHelper.ToLocation(ctx.Location),
                        Strip(firstIdentity.Value.FullyQualifiedName),
                        firstIdentity.Value.ModuleName,
                        Strip(ctx.FullyQualifiedName),
                        ctx.ModuleName
                    )
                );
            }
        }

        // SM0005: IdentityDbContext with wrong type args
        foreach (var ctx in data.DbContexts)
        {
            if (ctx.IsIdentityDbContext && string.IsNullOrEmpty(ctx.IdentityUserTypeFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.IdentityDbContextBadTypeArgs,
                        LocationHelper.ToLocation(ctx.Location),
                        Strip(ctx.FullyQualifiedName),
                        ctx.ModuleName,
                        0
                    )
                );
            }
        }

        // SM0055: Entity classes must live in a .Contracts assembly.
        // Walks every DbSet in the same pass that also collects EntityFqns
        // for SM0006 below, so we only iterate data.DbContexts once.
        var allEntityFqns = new HashSet<string>();
        foreach (var ctx in data.DbContexts)
        {
            foreach (var dbSet in ctx.DbSets)
            {
                allEntityFqns.Add(dbSet.EntityFqn);

                // Skip entities we can't flag: IdentityDbContext external types,
                // metadata-only symbols (no source location), and anything that
                // lives outside the SimpleModule.* assembly family.
                if (ctx.IsIdentityDbContext)
                    continue;
                if (dbSet.EntityLocation is null)
                    continue;
                if (
                    !dbSet.EntityAssemblyName.StartsWith(
                        AssemblyConventions.FrameworkPrefix,
                        StringComparison.Ordinal
                    )
                )
                    continue;
                if (
                    dbSet.EntityAssemblyName.EndsWith(
                        AssemblyConventions.ContractsSuffix,
                        StringComparison.Ordinal
                    )
                )
                    continue;

                var expectedContractsAssembly =
                    AssemblyConventions.GetExpectedContractsAssemblyName(dbSet.EntityAssemblyName);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EntityNotInContractsAssembly,
                        LocationHelper.ToLocation(dbSet.EntityLocation),
                        Strip(dbSet.EntityFqn),
                        dbSet.PropertyName,
                        Strip(ctx.FullyQualifiedName),
                        dbSet.EntityAssemblyName,
                        expectedContractsAssembly
                    )
                );
            }
        }

        // SM0006: Entity config for entity not in any DbSet
        // (allEntityFqns was populated above during the SM0055 pass)
        foreach (var config in data.EntityConfigs)
        {
            if (!allEntityFqns.Contains(config.EntityFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EntityConfigForMissingEntity,
                        LocationHelper.ToLocation(config.Location),
                        Strip(config.EntityFqn),
                        Strip(config.ConfigFqn),
                        config.ModuleName
                    )
                );
            }
        }

        // SM0007: Duplicate EntityTypeConfiguration for same entity
        var entityConfigOwners = new Dictionary<string, string>();
        foreach (var config in data.EntityConfigs)
        {
            if (entityConfigOwners.TryGetValue(config.EntityFqn, out var existing))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateEntityConfiguration,
                        LocationHelper.ToLocation(config.Location),
                        Strip(config.EntityFqn),
                        existing,
                        Strip(config.ConfigFqn)
                    )
                );
            }
            else
            {
                entityConfigOwners[config.EntityFqn] = Strip(config.ConfigFqn);
            }
        }

        // SM0010: Circular module dependency
        var (_, sortResult) = TopologicalSort.SortModulesWithResult(data);

        if (!sortResult.IsSuccess && sortResult.Cycle.Length > 0)
        {
            // Build cycle string: "A → B → C → A"
            var cycleNodes = new List<string>();
            foreach (var c in sortResult.Cycle)
                cycleNodes.Add(c);
            cycleNodes.Add(sortResult.Cycle[0]); // close the loop
            var cycleStr = string.Join(" \u2192 ", cycleNodes);

            // Build "how it happened" string
            var cycleSet = new HashSet<string>();
            foreach (var c in sortResult.Cycle)
                cycleSet.Add(c);

            var howParts = new List<string>();
            foreach (var dep in data.Dependencies)
            {
                if (cycleSet.Contains(dep.ModuleName) && cycleSet.Contains(dep.DependsOnModuleName))
                {
                    howParts.Add(
                        dep.ModuleName + " references " + dep.ContractsAssemblyName + ". "
                    );
                }
            }
            var howStr = string.Join("", howParts);

            var first = sortResult.Cycle[0];
            var second = sortResult.Cycle.Length > 1 ? sortResult.Cycle[1] : first;

            // Find location of the first module in the cycle
            SourceLocationRecord? cycleLoc = null;
            foreach (var module in data.Modules)
            {
                if (module.ModuleName == first)
                {
                    cycleLoc = module.Location;
                    break;
                }
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.CircularModuleDependency,
                    LocationHelper.ToLocation(cycleLoc),
                    cycleStr,
                    howStr,
                    first,
                    second
                )
            );
        }

        // SM0011: Illegal implementation references
        foreach (var illegal in data.IllegalReferences)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.IllegalImplementationReference,
                    LocationHelper.ToLocation(illegal.Location),
                    illegal.ReferencingModuleName,
                    illegal.ReferencedModuleName,
                    illegal.ReferencedAssemblyName
                )
            );
        }

        // SM0012/SM0013: Contract interface size
        foreach (var iface in data.ContractInterfaces)
        {
            if (iface.MethodCount > 20)
            {
                var shortName = ExtractShortName(iface.InterfaceName);
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ContractInterfaceTooLargeError,
                        LocationHelper.ToLocation(iface.Location),
                        Strip(iface.InterfaceName),
                        iface.MethodCount,
                        shortName
                    )
                );
            }
            else if (iface.MethodCount >= 15)
            {
                var shortName = ExtractShortName(iface.InterfaceName);
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ContractInterfaceTooLargeWarning,
                        LocationHelper.ToLocation(iface.Location),
                        Strip(iface.InterfaceName),
                        iface.MethodCount,
                        shortName
                    )
                );
            }
        }

        // SM0014: Missing contract interfaces in referenced contracts assemblies
        var contractsWithInterfaces = new HashSet<string>();
        foreach (var iface in data.ContractInterfaces)
            contractsWithInterfaces.Add(iface.ContractsAssemblyName);

        // Deduplicate: only report once per (module, contracts assembly) pair
        var reported = new HashSet<string>();
        foreach (var dep in data.Dependencies)
        {
            var key = dep.ModuleName + "|" + dep.ContractsAssemblyName;
            if (!contractsWithInterfaces.Contains(dep.ContractsAssemblyName) && reported.Add(key))
            {
                // Find the module's location for this diagnostic
                SourceLocationRecord? depModuleLoc = null;
                foreach (var module in data.Modules)
                {
                    if (module.ModuleName == dep.ModuleName)
                    {
                        depModuleLoc = module.Location;
                        break;
                    }
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MissingContractInterfaces,
                        LocationHelper.ToLocation(depModuleLoc),
                        dep.ModuleName,
                        dep.ContractsAssemblyName
                    )
                );
            }
        }

        // SM0025/SM0026/SM0028/SM0029: Contract implementation diagnostics
        // Group all implementations by interface FQN
        var implsByInterface = new Dictionary<string, List<ContractImplementationRecord>>();
        foreach (var impl in data.ContractImplementations)
        {
            if (!implsByInterface.TryGetValue(impl.InterfaceFqn, out var list))
            {
                list = new List<ContractImplementationRecord>();
                implsByInterface[impl.InterfaceFqn] = list;
            }
            list.Add(impl);
        }

        // SM0028: Non-public implementations
        // SM0029: Abstract implementations
        foreach (var impl in data.ContractImplementations)
        {
            if (!impl.IsPublic)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ContractImplementationNotPublic,
                        LocationHelper.ToLocation(impl.Location),
                        Strip(impl.ImplementationFqn),
                        Strip(impl.InterfaceFqn)
                    )
                );
            }

            if (impl.IsAbstract)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ContractImplementationIsAbstract,
                        LocationHelper.ToLocation(impl.Location),
                        Strip(impl.ImplementationFqn),
                        Strip(impl.InterfaceFqn)
                    )
                );
            }
        }

        // SM0025: No implementation for a contract interface
        foreach (var iface in data.ContractInterfaces)
        {
            if (!implsByInterface.ContainsKey(iface.InterfaceName))
            {
                // Derive module name from contracts assembly name
                var moduleName = iface.ContractsAssemblyName;
                if (moduleName.EndsWith(".Contracts", System.StringComparison.Ordinal))
                    moduleName = moduleName.Substring(0, moduleName.Length - ".Contracts".Length);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.NoContractImplementation,
                        LocationHelper.ToLocation(iface.Location),
                        Strip(iface.InterfaceName),
                        moduleName
                    )
                );
            }
        }

        // SM0026: Multiple valid implementations for the same interface
        foreach (var kvp in implsByInterface)
        {
            var validImpls = new List<ContractImplementationRecord>();
            foreach (var impl in kvp.Value)
            {
                if (impl.IsPublic && !impl.IsAbstract)
                    validImpls.Add(impl);
            }

            if (validImpls.Count > 1)
            {
                var names = new List<string>();
                foreach (var impl in validImpls)
                    names.Add(Strip(impl.ImplementationFqn));

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MultipleContractImplementations,
                        LocationHelper.ToLocation(validImpls[1].Location),
                        Strip(kvp.Key),
                        validImpls[0].ModuleName,
                        string.Join(", ", names)
                    )
                );
            }
        }

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

        // SM0035: DTO type in contracts with no public properties
        // Exclude permission and feature classes — they only have const string fields, not properties
        var permissionClassFqns = new HashSet<string>();
        foreach (var perm in data.PermissionClasses)
            permissionClassFqns.Add(perm.FullyQualifiedName);
        foreach (var feat in data.FeatureClasses)
            permissionClassFqns.Add(feat.FullyQualifiedName);

        foreach (var dto in data.DtoTypes)
        {
            if (
                dto.FullyQualifiedName.Contains(".Contracts.")
                && dto.Properties.Length == 0
                && !permissionClassFqns.Contains(dto.FullyQualifiedName)
            )
            {
                // Extract assembly/namespace context from FQN
                var fqn = Strip(dto.FullyQualifiedName);
                var contractsIdx = fqn.IndexOf(".Contracts.", System.StringComparison.Ordinal);
                var contractsAsm =
                    contractsIdx >= 0 ? fqn.Substring(0, contractsIdx + ".Contracts".Length) : fqn;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DtoTypeNoProperties,
                        Location.None,
                        fqn,
                        contractsAsm
                    )
                );
            }
        }

        // SM0038: Infrastructure type (DbContext subclass) in Contracts assembly
        foreach (var dto in data.DtoTypes)
        {
            if (
                dto.FullyQualifiedName.Contains(".Contracts.")
                && dto.FullyQualifiedName.Contains("DbContext")
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InfrastructureTypeInContracts,
                        Location.None,
                        Strip(dto.FullyQualifiedName)
                    )
                );
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

    private static string ExtractShortName(string interfaceName)
    {
        var name = Strip(interfaceName);
        if (name.Contains("."))
            name = name.Substring(name.LastIndexOf('.') + 1);
        if (name.StartsWith("I", System.StringComparison.Ordinal) && name.Length > 1)
            name = name.Substring(1);
        if (name.EndsWith("Contracts", System.StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Contracts".Length);
        return name;
    }
}
