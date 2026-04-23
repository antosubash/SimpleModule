using System.Collections.Immutable;
using System.Linq;

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
    ImmutableArray<string> ContractsAssemblyNames,
    bool HasAgentsAssembly,
    bool HasRagAssembly,
    string HostAssemblyName
)
{
    public bool HasAnyAgentContent =>
        HasAgentsAssembly
        && (
            AgentDefinitions.Length > 0
            || AgentToolProviders.Length > 0
            || KnowledgeSources.Length > 0
            || Modules.Any(m => m.HasConfigureAgents)
        );

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
        ImmutableArray<string>.Empty,
        false,
        false,
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
            && ContractsAssemblyNames.SequenceEqual(other.ContractsAssemblyNames)
            && HasAgentsAssembly == other.HasAgentsAssembly
            && HasRagAssembly == other.HasRagAssembly
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
        hash = HashHelper.HashArray(hash, ContractsAssemblyNames);
        hash = HashHelper.Combine(hash, HasAgentsAssembly.GetHashCode());
        hash = HashHelper.Combine(hash, HasRagAssembly.GetHashCode());
        hash = HashHelper.Combine(hash, (HostAssemblyName ?? "").GetHashCode());
        return hash;
    }
}

#endregion
