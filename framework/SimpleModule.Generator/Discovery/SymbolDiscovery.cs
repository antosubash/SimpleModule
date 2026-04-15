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

        // Discover IEndpoint and IViewEndpoint implementors per module assembly
        EndpointFinder.Discover(modules, moduleSymbols, s, cancellationToken);

        // Discover DbContext subclasses and IEntityTypeConfiguration<T> per module assembly
        var dbContexts = new List<DbContextInfo>();
        var entityConfigs = new List<EntityConfigInfo>();
        DbContextFinder.Discover(
            modules,
            moduleSymbols,
            dbContexts,
            entityConfigs,
            cancellationToken
        );

        var dtoTypes = new List<DtoTypeInfo>();
        DtoFinder.DiscoverAttributedDtos(compilation, s, dtoTypes, cancellationToken);

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

        // Step 3e: ISaveChangesInterceptor implementors
        var interceptors = new List<InterceptorInfo>();
        InterceptorFinder.Discover(modules, moduleSymbols, s, interceptors);

        // Step 3e': Vogen value objects with EF Core value converters
        var vogenValueObjects = new List<VogenValueObjectRecord>();
        VogenFinder.Discover(modules, moduleSymbols, contractsAssemblySymbols, vogenValueObjects);

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

        // Step 3g: Agent definitions, tool providers, knowledge sources
        var agentDefinitions = new List<DiscoveredTypeInfo>();
        var agentToolProviders = new List<DiscoveredTypeInfo>();
        var knowledgeSources = new List<DiscoveredTypeInfo>();
        AgentFinder.DiscoverAll(
            modules,
            moduleSymbols,
            s,
            agentDefinitions,
            agentToolProviders,
            knowledgeSources
        );

        // Step 4: Detect dependencies and illegal references
        var dependencies = new List<ModuleDependencyRecord>();
        var illegalReferences = new List<IllegalModuleReferenceRecord>();
        DependencyAnalyzer.Analyze(
            modules,
            moduleSymbols,
            moduleAssemblyMap,
            contractsAssemblyMap,
            dependencies,
            illegalReferences
        );

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
