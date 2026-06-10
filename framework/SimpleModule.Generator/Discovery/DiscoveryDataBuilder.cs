using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SimpleModule.Generator;

internal static class DiscoveryDataBuilder
{
    /// <summary>
    /// Converts the mutable working collections gathered during discovery into an
    /// equatable <see cref="DiscoveryData"/> record with <see cref="ImmutableArray{T}"/>
    /// fields. The equatable shape is required so the incremental generator pipeline
    /// can compare results and skip regenerating when nothing changed.
    /// </summary>
    internal static DiscoveryData Build(
        List<ModuleInfo> modules,
        List<DtoTypeInfo> dtoTypes,
        List<DbContextInfo> dbContexts,
        List<EntityConfigInfo> entityConfigs,
        List<ModuleDependencyRecord> dependencies,
        List<IllegalModuleReferenceRecord> illegalReferences,
        List<ContractInterfaceInfoRecord> contractInterfaces,
        List<ContractImplementationInfo> contractImplementations,
        List<PermissionClassInfo> permissionClasses,
        List<FeatureClassInfo> featureClasses,
        List<InterceptorInfo> interceptors,
        List<VogenValueObjectRecord> vogenValueObjects,
        List<ModuleOptionsRecord> moduleOptionsList,
        List<DiscoveredTypeInfo> agentDefinitions,
        List<DiscoveredTypeInfo> agentToolProviders,
        List<DiscoveredTypeInfo> knowledgeSources,
        List<FormRequestInfo> formRequests,
        List<EventTypeRecord> eventTypes,
        List<EventHandlerRecord> eventHandlers,
        Dictionary<string, string> contractsAssemblyMap,
        bool hasAgentsAssembly,
        bool hasRagAssembly,
        string hostAssemblyName,
        string coreAssemblyVersion
    )
    {
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
                    m.DisplayName,
                    m.Version,
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
                    c.IsManuallyRegistered,
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
            formRequests
                .Select(f => new FormRequestInfoRecord(
                    f.FullyQualifiedName,
                    f.IsSealed,
                    f.ExtendsFormRequest,
                    f.Location
                ))
                .ToImmutableArray(),
            eventTypes.ToImmutableArray(),
            eventHandlers.ToImmutableArray(),
            contractsAssemblyMap.Keys.ToImmutableArray(),
            hasAgentsAssembly,
            hasRagAssembly,
            hostAssemblyName,
            coreAssemblyVersion
        );
    }
}
