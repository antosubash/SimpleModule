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
