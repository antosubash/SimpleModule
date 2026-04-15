using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SimpleModule.Generator;

internal readonly record struct DtoTypeInfoRecord(
    string FullyQualifiedName,
    string SafeName,
    string? BaseTypeFqn,
    ImmutableArray<DtoPropertyInfoRecord> Properties
)
{
    public bool Equals(DtoTypeInfoRecord other)
    {
        return FullyQualifiedName == other.FullyQualifiedName
            && SafeName == other.SafeName
            && BaseTypeFqn == other.BaseTypeFqn
            && Properties.SequenceEqual(other.Properties);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, SafeName.GetHashCode());
        hash = HashHelper.Combine(hash, BaseTypeFqn?.GetHashCode() ?? 0);
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

internal readonly record struct DbSetInfoRecord(
    string PropertyName,
    string EntityFqn,
    string EntityAssemblyName,
    SourceLocationRecord? EntityLocation
);

internal readonly record struct EntityConfigInfoRecord(
    string ConfigFqn,
    string EntityFqn,
    string ModuleName,
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

internal sealed class ModuleInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public string AssemblyName { get; set; } = "";
    public bool HasConfigureServices { get; set; }
    public bool HasConfigureEndpoints { get; set; }
    public bool HasConfigureMenu { get; set; }
    public bool HasConfigurePermissions { get; set; }
    public bool HasConfigureMiddleware { get; set; }
    public bool HasConfigureSettings { get; set; }
    public bool HasConfigureFeatureFlags { get; set; }
    public bool HasConfigureAgents { get; set; }
    public bool HasConfigureRateLimits { get; set; }
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
    public string RouteTemplate { get; set; } = "";
    public string HttpMethod { get; set; } = "";
}

internal sealed class ViewInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string? Page { get; set; }
    public string InferredClassName { get; set; } = "";
    public string RouteTemplate { get; set; } = "";
    public SourceLocationRecord? Location { get; set; }
}

internal sealed class DtoTypeInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string SafeName { get; set; } = "";
    public string? BaseTypeFqn { get; set; }
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
    public string EntityAssemblyName { get; set; } = "";
    public SourceLocationRecord? EntityLocation { get; set; }
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
