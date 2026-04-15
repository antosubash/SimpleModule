using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class SymbolDiscovery
{
    internal static DiscoveryData Extract(
        Compilation compilation,
        CancellationToken cancellationToken
    )
    {
        var hostAssemblyName = compilation.Assembly.Name;

        var symbols = CoreSymbols.TryResolve(compilation);
        if (symbols is null)
            return DiscoveryData.Empty;
        var s = symbols.Value;

        var modules = new List<ModuleInfo>();

        foreach (var reference in compilation.References)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (
                compilation.GetAssemblyOrModuleSymbol(reference)
                is not IAssemblySymbol assemblySymbol
            )
                continue;

            ModuleFinder.FindModuleTypes(
                assemblySymbol.GlobalNamespace,
                s,
                modules,
                cancellationToken
            );
        }

        ModuleFinder.FindModuleTypes(
            compilation.Assembly.GlobalNamespace,
            s,
            modules,
            cancellationToken
        );

        if (modules.Count == 0)
            return DiscoveryData.Empty;

        // Resolve each module's type symbol once — avoids repeated GetTypeByMetadataName calls.
        var moduleSymbols = new Dictionary<string, INamedTypeSymbol>();
        foreach (var module in modules)
        {
            var metadataName = TypeMappingHelpers.StripGlobalPrefix(module.FullyQualifiedName);
            var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
            if (typeSymbol is not null)
                moduleSymbols[module.FullyQualifiedName] = typeSymbol;
        }

        // Discover IEndpoint implementors per module assembly.
        // Classification is by interface type: IViewEndpoint -> view, IEndpoint -> API.
        // Scan each assembly once, then match endpoints to the closest module by namespace.
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
            EndpointFinder.FindEndpointTypes(
                assembly.GlobalNamespace,
                s,
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

        // Discover DbContext subclasses and IEntityTypeConfiguration<T> per module assembly.
        // Scan each assembly once, then match DbContexts/configs to the nearest module by namespace.
        var dbContexts = new List<DbContextInfo>();
        var entityConfigs = new List<EntityConfigInfo>();
        var scannedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var module in modules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var assembly = typeSymbol.ContainingAssembly;
            if (!scannedAssemblies.Add(assembly))
                continue;

            // Collect unmatched items from this assembly
            var rawDbContexts = new List<DbContextInfo>();
            var rawEntityConfigs = new List<EntityConfigInfo>();
            DbContextFinder.FindDbContextTypes(
                assembly.GlobalNamespace,
                "",
                rawDbContexts,
                cancellationToken
            );
            DbContextFinder.FindEntityConfigTypes(
                assembly.GlobalNamespace,
                "",
                rawEntityConfigs,
                cancellationToken
            );

            // Match each DbContext to the module whose namespace is closest
            foreach (var ctx in rawDbContexts)
            {
                var ctxNs = TypeMappingHelpers.StripGlobalPrefix(ctx.FullyQualifiedName);
                ctx.ModuleName = SymbolHelpers.FindClosestModuleName(ctxNs, modules);
                dbContexts.Add(ctx);
            }

            foreach (var cfg in rawEntityConfigs)
            {
                var cfgNs = TypeMappingHelpers.StripGlobalPrefix(cfg.ConfigFqn);
                cfg.ModuleName = SymbolHelpers.FindClosestModuleName(cfgNs, modules);
                entityConfigs.Add(cfg);
            }
        }

        var dtoTypes = new List<DtoTypeInfo>();
        if (s.DtoAttribute is not null)
        {
            foreach (var reference in compilation.References)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (
                    compilation.GetAssemblyOrModuleSymbol(reference)
                    is not IAssemblySymbol assemblySymbol
                )
                    continue;

                DtoFinder.FindDtoTypes(
                    assemblySymbol.GlobalNamespace,
                    s.DtoAttribute,
                    dtoTypes,
                    cancellationToken
                );
            }

            DtoFinder.FindDtoTypes(
                compilation.Assembly.GlobalNamespace,
                s.DtoAttribute,
                dtoTypes,
                cancellationToken
            );
        }

        // --- Dependency inference ---
        cancellationToken.ThrowIfCancellationRequested();

        // Step 1: Build module assembly map (assembly name → module name)
        var moduleAssemblyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var assemblyName = typeSymbol.ContainingAssembly.Name;
            if (!moduleAssemblyMap.ContainsKey(assemblyName))
                moduleAssemblyMap[assemblyName] = module.ModuleName;
        }

        // Step 2: Build contracts-to-module map
        var contractsAssemblyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var contractsAssemblySymbols = new Dictionary<string, IAssemblySymbol>(
            StringComparer.OrdinalIgnoreCase
        );
        ContractFinder.BuildContractsAssemblyMap(
            compilation,
            moduleAssemblyMap,
            contractsAssemblyMap,
            contractsAssemblySymbols
        );

        // Convention-based DTO discovery: all public types in *.Contracts assemblies
        var existingDtoFqns = new HashSet<string>();
        foreach (var d in dtoTypes)
            existingDtoFqns.Add(d.FullyQualifiedName);

        foreach (var kvp in contractsAssemblySymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DtoFinder.FindConventionDtoTypes(
                kvp.Value.GlobalNamespace,
                s.NoDtoAttribute,
                s.EventInterface,
                existingDtoFqns,
                dtoTypes,
                cancellationToken
            );
        }

        // Step 3/3b: Contract interfaces and implementations
        var contractInterfaces = new List<ContractInterfaceInfoRecord>();
        var contractImplementations = new List<ContractImplementationInfo>();
        ContractFinder.DiscoverInterfacesAndImplementations(
            modules,
            moduleSymbols,
            contractsAssemblySymbols,
            compilation,
            contractInterfaces,
            contractImplementations
        );

        // Step 3c: Find IModulePermissions implementors in module and contracts assemblies
        var permissionClasses = new List<PermissionClassInfo>();
        PermissionFeatureFinder.DiscoverPermissions(
            modules,
            moduleSymbols,
            contractsAssemblySymbols,
            contractsAssemblyMap,
            s,
            permissionClasses
        );

        // Step 3d: Find IModuleFeatures implementors in module and contracts assemblies
        var featureClasses = new List<FeatureClassInfo>();
        PermissionFeatureFinder.DiscoverFeatures(
            modules,
            moduleSymbols,
            contractsAssemblySymbols,
            contractsAssemblyMap,
            s,
            featureClasses
        );

        // Step 3e: Find ISaveChangesInterceptor implementors in module assemblies
        var interceptors = new List<InterceptorInfo>();
        if (s.SaveChangesInterceptor is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                {
                    InterceptorFinder.FindInterceptorTypes(
                        assembly.GlobalNamespace,
                        s.SaveChangesInterceptor,
                        module.ModuleName,
                        interceptors
                    );
                }
            );
        }

        // Step 3e: Discover Vogen value objects with EF Core value converters.
        // Scan Contracts assemblies and module assemblies only.
        var vogenValueObjects = new List<VogenValueObjectRecord>();
        var voScannedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        foreach (var kvp in contractsAssemblySymbols)
        {
            if (voScannedAssemblies.Add(kvp.Value))
            {
                VogenFinder.FindVogenValueObjectsWithEfConverters(
                    kvp.Value.GlobalNamespace,
                    vogenValueObjects
                );
            }
        }

        SymbolHelpers.ScanModuleAssemblies(
            modules,
            moduleSymbols,
            (assembly, _) =>
            {
                if (voScannedAssemblies.Add(assembly))
                {
                    VogenFinder.FindVogenValueObjectsWithEfConverters(
                        assembly.GlobalNamespace,
                        vogenValueObjects
                    );
                }
            }
        );

        // Step 3f: Find IModuleOptions implementors in module and contracts assemblies
        var moduleOptionsList = new List<ModuleOptionsRecord>();
        PermissionFeatureFinder.DiscoverModuleOptions(
            modules,
            moduleSymbols,
            contractsAssemblySymbols,
            contractsAssemblyMap,
            s,
            moduleOptionsList
        );

        // Step 3g: Find IAgentDefinition, IAgentToolProvider, and IKnowledgeSource implementors
        var agentDefinitions = new List<DiscoveredTypeInfo>();
        var agentToolProviders = new List<DiscoveredTypeInfo>();
        var knowledgeSources = new List<DiscoveredTypeInfo>();

        if (s.AgentDefinition is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    AgentFinder.FindImplementors(
                        assembly.GlobalNamespace,
                        s.AgentDefinition,
                        module.ModuleName,
                        agentDefinitions
                    )
            );
        }

        if (s.AgentToolProvider is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    AgentFinder.FindImplementors(
                        assembly.GlobalNamespace,
                        s.AgentToolProvider,
                        module.ModuleName,
                        agentToolProviders
                    )
            );
        }

        if (s.KnowledgeSource is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    AgentFinder.FindImplementors(
                        assembly.GlobalNamespace,
                        s.KnowledgeSource,
                        module.ModuleName,
                        knowledgeSources
                    )
            );
        }

        // Step 4: Detect dependencies and illegal references
        var dependencies = new List<ModuleDependencyRecord>();
        var illegalReferences = new List<IllegalModuleReferenceRecord>();

        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var moduleAssembly = typeSymbol.ContainingAssembly;
            var thisModuleName = module.ModuleName;

            foreach (var asmModule in moduleAssembly.Modules)
            {
                foreach (var referencedAsm in asmModule.ReferencedAssemblySymbols)
                {
                    var refName = referencedAsm.Name;

                    // Check for illegal direct module-to-module reference
                    if (
                        moduleAssemblyMap.TryGetValue(refName, out var referencedModuleName)
                        && !string.Equals(
                            referencedModuleName,
                            thisModuleName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        illegalReferences.Add(
                            new IllegalModuleReferenceRecord(
                                thisModuleName,
                                moduleAssembly.Name,
                                referencedModuleName,
                                refName,
                                module.Location
                            )
                        );
                    }

                    // Check for dependency via contracts
                    if (
                        contractsAssemblyMap.TryGetValue(refName, out var depModuleName)
                        && !string.Equals(
                            depModuleName,
                            thisModuleName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        dependencies.Add(
                            new ModuleDependencyRecord(thisModuleName, depModuleName, refName)
                        );
                    }
                }
            }
        }

        return new DiscoveryData(
            modules
                .Select(m => new ModuleInfoRecord(
                    m.FullyQualifiedName,
                    m.ModuleName,
                    m.AssemblyName,
                    m.HasConfigureServices,
                    m.HasConfigureEndpoints,
                    m.HasConfigureMenu,
                    m.HasConfigurePermissions,
                    m.HasConfigureMiddleware,
                    m.HasConfigureSettings,
                    m.HasConfigureFeatureFlags,
                    m.HasConfigureAgents,
                    m.HasConfigureRateLimits,
                    m.RoutePrefix,
                    m.ViewPrefix,
                    m.Endpoints.Select(e => new EndpointInfoRecord(
                            e.FullyQualifiedName,
                            e.RequiredPermissions.ToImmutableArray(),
                            e.AllowAnonymous,
                            e.RouteTemplate,
                            e.HttpMethod
                        ))
                        .ToImmutableArray(),
                    m.Views.Select(v => new ViewInfoRecord(
                            v.FullyQualifiedName,
                            v.Page ?? "",
                            v.RouteTemplate,
                            v.Location
                        ))
                        .ToImmutableArray(),
                    m.Location
                ))
                .ToImmutableArray(),
            dtoTypes
                .Select(d => new DtoTypeInfoRecord(
                    d.FullyQualifiedName,
                    d.SafeName,
                    d.BaseTypeFqn,
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
                    c.DbSets.Select(d => new DbSetInfoRecord(
                            d.PropertyName,
                            d.EntityFqn,
                            d.EntityAssemblyName,
                            d.EntityLocation
                        ))
                        .ToImmutableArray(),
                    c.Location
                ))
                .ToImmutableArray(),
            entityConfigs
                .Select(e => new EntityConfigInfoRecord(
                    e.ConfigFqn,
                    e.EntityFqn,
                    e.ModuleName,
                    e.Location
                ))
                .ToImmutableArray(),
            dependencies.ToImmutableArray(),
            illegalReferences.ToImmutableArray(),
            contractInterfaces.ToImmutableArray(),
            contractImplementations
                .Select(c => new ContractImplementationRecord(
                    c.InterfaceFqn,
                    c.ImplementationFqn,
                    c.ModuleName,
                    c.IsPublic,
                    c.IsAbstract,
                    c.DependsOnDbContext,
                    c.Location,
                    c.Lifetime
                ))
                .ToImmutableArray(),
            permissionClasses
                .Select(p => new PermissionClassRecord(
                    p.FullyQualifiedName,
                    p.ModuleName,
                    p.IsSealed,
                    p.Fields.Select(f => new PermissionFieldRecord(
                            f.FieldName,
                            f.Value,
                            f.IsConstString,
                            f.Location
                        ))
                        .ToImmutableArray(),
                    p.Location
                ))
                .ToImmutableArray(),
            featureClasses
                .Select(f => new FeatureClassRecord(
                    f.FullyQualifiedName,
                    f.ModuleName,
                    f.IsSealed,
                    f.Fields.Select(ff => new FeatureFieldRecord(
                            ff.FieldName,
                            ff.Value,
                            ff.IsConstString,
                            ff.Location
                        ))
                        .ToImmutableArray(),
                    f.Location
                ))
                .ToImmutableArray(),
            interceptors
                .Select(i => new InterceptorInfoRecord(
                    i.FullyQualifiedName,
                    i.ModuleName,
                    i.ConstructorParamTypeFqns.ToImmutableArray(),
                    i.Location
                ))
                .ToImmutableArray(),
            vogenValueObjects.ToImmutableArray(),
            moduleOptionsList.ToImmutableArray(),
            agentDefinitions
                .Select(a => new AgentDefinitionRecord(a.FullyQualifiedName, a.ModuleName))
                .ToImmutableArray(),
            agentToolProviders
                .Select(a => new AgentToolProviderRecord(a.FullyQualifiedName, a.ModuleName))
                .ToImmutableArray(),
            knowledgeSources
                .Select(k => new KnowledgeSourceRecord(k.FullyQualifiedName, k.ModuleName))
                .ToImmutableArray(),
            contractsAssemblyMap.Keys.ToImmutableArray(),
            s.HasAgentsAssembly,
            hostAssemblyName
        );
    }
}
