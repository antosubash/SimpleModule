using System.Collections.Generic;
using System.Collections.Immutable;

namespace SimpleModule.Generator;

internal static class HashHelper
{
    internal static int Combine(int hash, int value) => unchecked(hash * 31 + value);

    internal static int HashArray<T>(int hash, ImmutableArray<T> items)
    {
        foreach (var item in items)
            hash = Combine(hash, item!.GetHashCode());
        return hash;
    }
}

/// <summary>
/// Serializable source location for incremental caching.
/// Unlike <see cref="Microsoft.CodeAnalysis.Location"/>, this record is fully equatable
/// and does not hold references to syntax trees, making it safe for incremental pipelines.
/// </summary>
internal readonly record struct SourceLocationRecord(
    string FilePath,
    int StartLine,
    int StartCharacter,
    int EndLine,
    int EndCharacter
);

#region Equatable data model for incremental caching

internal readonly record struct DiscoveryData(
    ImmutableArray<ModuleInfoRecord> Modules,
    ImmutableArray<DtoTypeInfoRecord> DtoTypes,
    ImmutableArray<DbContextInfoRecord> DbContexts,
    ImmutableArray<EntityConfigInfoRecord> EntityConfigs,
    ImmutableArray<ModuleDependencyRecord> Dependencies,
    ImmutableArray<IllegalModuleReferenceRecord> IllegalReferences,
    ImmutableArray<ContractInterfaceInfoRecord> ContractInterfaces,
    ImmutableArray<ContractImplementationRecord> ContractImplementations,
    ImmutableArray<PermissionClassRecord> PermissionClasses,
    ImmutableArray<FeatureClassRecord> FeatureClasses,
    ImmutableArray<InterceptorInfoRecord> Interceptors,
    ImmutableArray<VogenValueObjectRecord> VogenValueObjects,
    ImmutableArray<ModuleOptionsRecord> ModuleOptions,
    ImmutableArray<AgentDefinitionRecord> AgentDefinitions,
    ImmutableArray<AgentToolProviderRecord> AgentToolProviders,
    ImmutableArray<KnowledgeSourceRecord> KnowledgeSources,
    string HostAssemblyName
)
{
    public static readonly DiscoveryData Empty = new(
        ImmutableArray<ModuleInfoRecord>.Empty,
        ImmutableArray<DtoTypeInfoRecord>.Empty,
        ImmutableArray<DbContextInfoRecord>.Empty,
        ImmutableArray<EntityConfigInfoRecord>.Empty,
        ImmutableArray<ModuleDependencyRecord>.Empty,
        ImmutableArray<IllegalModuleReferenceRecord>.Empty,
        ImmutableArray<ContractInterfaceInfoRecord>.Empty,
        ImmutableArray<ContractImplementationRecord>.Empty,
        ImmutableArray<PermissionClassRecord>.Empty,
        ImmutableArray<FeatureClassRecord>.Empty,
        ImmutableArray<InterceptorInfoRecord>.Empty,
        ImmutableArray<VogenValueObjectRecord>.Empty,
        ImmutableArray<ModuleOptionsRecord>.Empty,
        ImmutableArray<AgentDefinitionRecord>.Empty,
        ImmutableArray<AgentToolProviderRecord>.Empty,
        ImmutableArray<KnowledgeSourceRecord>.Empty,
        ""
    );

    public bool Equals(DiscoveryData other)
    {
        return Modules.SequenceEqual(other.Modules)
            && DtoTypes.SequenceEqual(other.DtoTypes)
            && DbContexts.SequenceEqual(other.DbContexts)
            && EntityConfigs.SequenceEqual(other.EntityConfigs)
            && Dependencies.SequenceEqual(other.Dependencies)
            && IllegalReferences.SequenceEqual(other.IllegalReferences)
            && ContractInterfaces.SequenceEqual(other.ContractInterfaces)
            && ContractImplementations.SequenceEqual(other.ContractImplementations)
            && PermissionClasses.SequenceEqual(other.PermissionClasses)
            && FeatureClasses.SequenceEqual(other.FeatureClasses)
            && Interceptors.SequenceEqual(other.Interceptors)
            && VogenValueObjects.SequenceEqual(other.VogenValueObjects)
            && ModuleOptions.SequenceEqual(other.ModuleOptions)
            && AgentDefinitions.SequenceEqual(other.AgentDefinitions)
            && AgentToolProviders.SequenceEqual(other.AgentToolProviders)
            && KnowledgeSources.SequenceEqual(other.KnowledgeSources)
            && HostAssemblyName == other.HostAssemblyName;
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.HashArray(hash, Modules);
        hash = HashHelper.HashArray(hash, DtoTypes);
        hash = HashHelper.HashArray(hash, DbContexts);
        hash = HashHelper.HashArray(hash, EntityConfigs);
        hash = HashHelper.HashArray(hash, Dependencies);
        hash = HashHelper.HashArray(hash, IllegalReferences);
        hash = HashHelper.HashArray(hash, ContractInterfaces);
        hash = HashHelper.HashArray(hash, ContractImplementations);
        hash = HashHelper.HashArray(hash, PermissionClasses);
        hash = HashHelper.HashArray(hash, FeatureClasses);
        hash = HashHelper.HashArray(hash, Interceptors);
        hash = HashHelper.HashArray(hash, VogenValueObjects);
        hash = HashHelper.HashArray(hash, ModuleOptions);
        hash = HashHelper.HashArray(hash, AgentDefinitions);
        hash = HashHelper.HashArray(hash, AgentToolProviders);
        hash = HashHelper.HashArray(hash, KnowledgeSources);
        hash = HashHelper.Combine(hash, (HostAssemblyName ?? "").GetHashCode());
        return hash;
    }
}

internal readonly record struct ModuleInfoRecord(
    string FullyQualifiedName,
    string ModuleName,
    bool HasConfigureServices,
    bool HasConfigureEndpoints,
    bool HasConfigureMenu,
    bool HasConfigurePermissions,
    bool HasConfigureMiddleware,
    bool HasConfigureSettings,
    bool HasConfigureFeatureFlags,
    bool HasConfigureAgents,
    bool HasRazorComponents,
    string RoutePrefix,
    string ViewPrefix,
    ImmutableArray<EndpointInfoRecord> Endpoints,
    ImmutableArray<ViewInfoRecord> Views,
    SourceLocationRecord? Location
)
{
    public bool Equals(ModuleInfoRecord other)
    {
        return FullyQualifiedName == other.FullyQualifiedName
            && ModuleName == other.ModuleName
            && HasConfigureServices == other.HasConfigureServices
            && HasConfigureEndpoints == other.HasConfigureEndpoints
            && HasConfigureMenu == other.HasConfigureMenu
            && HasConfigureMiddleware == other.HasConfigureMiddleware
            && HasConfigurePermissions == other.HasConfigurePermissions
            && HasConfigureSettings == other.HasConfigureSettings
            && HasConfigureFeatureFlags == other.HasConfigureFeatureFlags
            && HasConfigureAgents == other.HasConfigureAgents
            && HasRazorComponents == other.HasRazorComponents
            && RoutePrefix == other.RoutePrefix
            && ViewPrefix == other.ViewPrefix
            && Endpoints.SequenceEqual(other.Endpoints)
            && Views.SequenceEqual(other.Views)
            && Location == other.Location;
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, (ModuleName ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureServices.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureEndpoints.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureMenu.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureMiddleware.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigurePermissions.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureSettings.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureFeatureFlags.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureAgents.GetHashCode());
        hash = HashHelper.Combine(hash, HasRazorComponents.GetHashCode());
        hash = HashHelper.Combine(hash, (RoutePrefix ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, (ViewPrefix ?? "").GetHashCode());
        hash = HashHelper.HashArray(hash, Endpoints);
        hash = HashHelper.HashArray(hash, Views);
        hash = HashHelper.Combine(hash, Location.GetHashCode());
        return hash;
    }
}

internal readonly record struct EndpointInfoRecord(
    string FullyQualifiedName,
    ImmutableArray<string> RequiredPermissions,
    bool AllowAnonymous
)
{
    public bool Equals(EndpointInfoRecord other)
    {
        return FullyQualifiedName == other.FullyQualifiedName
            && AllowAnonymous == other.AllowAnonymous
            && RequiredPermissions.SequenceEqual(other.RequiredPermissions);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, AllowAnonymous.GetHashCode());
        hash = HashHelper.HashArray(hash, RequiredPermissions);
        return hash;
    }
}

internal readonly record struct ViewInfoRecord(
    string FullyQualifiedName,
    string Page,
    SourceLocationRecord? Location
);

internal readonly record struct DtoTypeInfoRecord(
    string FullyQualifiedName,
    string SafeName,
    ImmutableArray<DtoPropertyInfoRecord> Properties
)
{
    public bool Equals(DtoTypeInfoRecord other)
    {
        return FullyQualifiedName == other.FullyQualifiedName
            && SafeName == other.SafeName
            && Properties.SequenceEqual(other.Properties);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, SafeName.GetHashCode());
        hash = HashHelper.HashArray(hash, Properties);
        return hash;
    }
}

internal readonly record struct DtoPropertyInfoRecord(
    string Name,
    string TypeFqn,
    string? UnderlyingTypeFqn,
    bool HasSetter
);

internal readonly record struct DbContextInfoRecord(
    string FullyQualifiedName,
    string ModuleName,
    bool IsIdentityDbContext,
    string IdentityUserTypeFqn,
    string IdentityRoleTypeFqn,
    string IdentityKeyTypeFqn,
    ImmutableArray<DbSetInfoRecord> DbSets,
    SourceLocationRecord? Location
)
{
    public bool Equals(DbContextInfoRecord other)
    {
        return FullyQualifiedName == other.FullyQualifiedName
            && ModuleName == other.ModuleName
            && IsIdentityDbContext == other.IsIdentityDbContext
            && IdentityUserTypeFqn == other.IdentityUserTypeFqn
            && IdentityRoleTypeFqn == other.IdentityRoleTypeFqn
            && IdentityKeyTypeFqn == other.IdentityKeyTypeFqn
            && DbSets.SequenceEqual(other.DbSets)
            && Location == other.Location;
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, (ModuleName ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, IsIdentityDbContext.GetHashCode());
        hash = HashHelper.Combine(hash, (IdentityUserTypeFqn ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, (IdentityRoleTypeFqn ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, (IdentityKeyTypeFqn ?? "").GetHashCode());
        hash = HashHelper.HashArray(hash, DbSets);
        hash = HashHelper.Combine(hash, Location.GetHashCode());
        return hash;
    }
}

internal readonly record struct DbSetInfoRecord(string PropertyName, string EntityFqn);

internal readonly record struct EntityConfigInfoRecord(
    string ConfigFqn,
    string EntityFqn,
    string ModuleName,
    SourceLocationRecord? Location
);

internal readonly record struct ModuleDependencyRecord(
    string ModuleName,
    string DependsOnModuleName,
    string ContractsAssemblyName
);

internal readonly record struct IllegalModuleReferenceRecord(
    string ReferencingModuleName,
    string ReferencingAssemblyName,
    string ReferencedModuleName,
    string ReferencedAssemblyName,
    SourceLocationRecord? Location
);

internal readonly record struct ContractInterfaceInfoRecord(
    string ContractsAssemblyName,
    string InterfaceName,
    int MethodCount,
    SourceLocationRecord? Location
);

internal readonly record struct ContractImplementationRecord(
    string InterfaceFqn,
    string ImplementationFqn,
    string ModuleName,
    bool IsPublic,
    bool IsAbstract,
    bool DependsOnDbContext,
    SourceLocationRecord? Location,
    int Lifetime
);

internal readonly record struct PermissionClassRecord(
    string FullyQualifiedName,
    string ModuleName,
    bool IsSealed,
    ImmutableArray<PermissionFieldRecord> Fields,
    SourceLocationRecord? Location
)
{
    public bool Equals(PermissionClassRecord other) =>
        FullyQualifiedName == other.FullyQualifiedName
        && ModuleName == other.ModuleName
        && IsSealed == other.IsSealed
        && Fields.SequenceEqual(other.Fields)
        && Location == other.Location;

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, (ModuleName ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, IsSealed.GetHashCode());
        hash = HashHelper.HashArray(hash, Fields);
        hash = HashHelper.Combine(hash, Location.GetHashCode());
        return hash;
    }
}

internal readonly record struct PermissionFieldRecord(
    string FieldName,
    string Value,
    bool IsConstString,
    SourceLocationRecord? Location
);

internal readonly record struct FeatureClassRecord(
    string FullyQualifiedName,
    string ModuleName,
    bool IsSealed,
    ImmutableArray<FeatureFieldRecord> Fields,
    SourceLocationRecord? Location
)
{
    public bool Equals(FeatureClassRecord other) =>
        FullyQualifiedName == other.FullyQualifiedName
        && ModuleName == other.ModuleName
        && IsSealed == other.IsSealed
        && Fields.SequenceEqual(other.Fields)
        && Location == other.Location;

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, (ModuleName ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, IsSealed.GetHashCode());
        hash = HashHelper.HashArray(hash, Fields);
        hash = HashHelper.Combine(hash, Location.GetHashCode());
        return hash;
    }
}

internal readonly record struct FeatureFieldRecord(
    string FieldName,
    string Value,
    bool IsConstString,
    SourceLocationRecord? Location
);

internal readonly record struct InterceptorInfoRecord(
    string FullyQualifiedName,
    string ModuleName,
    ImmutableArray<string> ConstructorParamTypeFqns,
    SourceLocationRecord? Location
)
{
    public bool Equals(InterceptorInfoRecord other) =>
        FullyQualifiedName == other.FullyQualifiedName
        && ModuleName == other.ModuleName
        && ConstructorParamTypeFqns.SequenceEqual(other.ConstructorParamTypeFqns)
        && Location == other.Location;

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, (ModuleName ?? "").GetHashCode());
        hash = HashHelper.HashArray(hash, ConstructorParamTypeFqns);
        hash = HashHelper.Combine(hash, Location.GetHashCode());
        return hash;
    }
}

internal readonly record struct ModuleOptionsRecord(
    string FullyQualifiedName,
    string ModuleName,
    SourceLocationRecord? Location
)
{
    internal static Dictionary<string, List<ModuleOptionsRecord>> GroupByModule(
        ImmutableArray<ModuleOptionsRecord> options
    )
    {
        var result = new Dictionary<string, List<ModuleOptionsRecord>>();
        foreach (var opt in options)
        {
            if (!result.TryGetValue(opt.ModuleName, out var list))
            {
                list = new List<ModuleOptionsRecord>();
                result[opt.ModuleName] = list;
            }
            list.Add(opt);
        }
        return result;
    }
}

internal readonly record struct VogenValueObjectRecord(
    string TypeFqn,
    string ConverterFqn,
    string ComparerFqn
);

internal readonly record struct AgentDefinitionRecord(string FullyQualifiedName, string ModuleName);

internal readonly record struct AgentToolProviderRecord(
    string FullyQualifiedName,
    string ModuleName
);

internal readonly record struct KnowledgeSourceRecord(string FullyQualifiedName, string ModuleName);

#endregion

#region Mutable working types (used during symbol traversal only)

internal sealed class ModuleInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool HasConfigureServices { get; set; }
    public bool HasConfigureEndpoints { get; set; }
    public bool HasConfigureMenu { get; set; }
    public bool HasConfigurePermissions { get; set; }
    public bool HasConfigureMiddleware { get; set; }
    public bool HasConfigureSettings { get; set; }
    public bool HasConfigureFeatureFlags { get; set; }
    public bool HasConfigureAgents { get; set; }
    public bool HasRazorComponents { get; set; }
    public string RoutePrefix { get; set; } = "";
    public string ViewPrefix { get; set; } = "";
    public List<EndpointInfo> Endpoints { get; set; } = new();
    public List<ViewInfo> Views { get; set; } = new();
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class EndpointInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public List<string> RequiredPermissions { get; set; } = new();
    public bool AllowAnonymous { get; set; }
}

internal sealed class ViewInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string? Page { get; set; }
    public string InferredClassName { get; set; } = "";
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class DtoTypeInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string SafeName { get; set; } = "";
    public List<DtoPropertyInfo> Properties { get; set; } = new();
}

internal sealed class DtoPropertyInfo
{
    public string Name { get; set; } = "";
    public string TypeFqn { get; set; } = "";

    /// <summary>
    /// For value objects (e.g. Vogen), the underlying primitive type FQN.
    /// Null if the type is not a value object wrapper.
    /// </summary>
    public string? UnderlyingTypeFqn { get; set; }

    public bool HasSetter { get; set; }
}

internal sealed class DbContextInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsIdentityDbContext { get; set; }
    public string IdentityUserTypeFqn { get; set; } = "";
    public string IdentityRoleTypeFqn { get; set; } = "";
    public string IdentityKeyTypeFqn { get; set; } = "";
    public List<DbSetInfo> DbSets { get; set; } = new();
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class DbSetInfo
{
    public string PropertyName { get; set; } = "";
    public string EntityFqn { get; set; } = "";
}

internal sealed class EntityConfigInfo
{
    public string ConfigFqn { get; set; } = "";
    public string EntityFqn { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class ContractImplementationInfo
{
    public string InterfaceFqn { get; set; } = "";
    public string ImplementationFqn { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsAbstract { get; set; }
    public bool DependsOnDbContext { get; set; }
    public SourceLocationRecord? Location { get; set; }
    public int Lifetime { get; set; } = 1; // Default: Scoped (ServiceLifetime.Scoped = 1)
}

internal sealed class PermissionClassInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsSealed { get; set; }
    public List<PermissionFieldInfo> Fields { get; set; } = new();
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class PermissionFieldInfo
{
    public string FieldName { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsConstString { get; set; }
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class FeatureClassInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsSealed { get; set; }
    public List<FeatureFieldInfo> Fields { get; set; } = new();
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class FeatureFieldInfo
{
    public string FieldName { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsConstString { get; set; }
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class InterceptorInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public List<string> ConstructorParamTypeFqns { get; set; } = new();
    public SourceLocationRecord? Location { get; set; }
}

/// <summary>
/// Shared mutable working type for discovered interface implementors (agents, tool providers, knowledge sources).
/// </summary>
internal sealed class DiscoveredTypeInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
}

#endregion
