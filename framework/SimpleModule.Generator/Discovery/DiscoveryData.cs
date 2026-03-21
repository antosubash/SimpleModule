using System.Collections.Generic;
using System.Collections.Immutable;

namespace SimpleModule.Generator;

#region Equatable data model for incremental caching

// These record types implement value equality so the incremental generator
// pipeline can detect when the extracted data hasn't changed and skip
// re-generating source files.

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
    ImmutableArray<InterceptorInfoRecord> Interceptors
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
        ImmutableArray<InterceptorInfoRecord>.Empty
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
            && Interceptors.SequenceEqual(other.Interceptors);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var m in Modules)
                hash = hash * 31 + m.GetHashCode();
            foreach (var d in DtoTypes)
                hash = hash * 31 + d.GetHashCode();
            foreach (var c in DbContexts)
                hash = hash * 31 + c.GetHashCode();
            foreach (var e in EntityConfigs)
                hash = hash * 31 + e.GetHashCode();
            foreach (var dep in Dependencies)
                hash = hash * 31 + dep.GetHashCode();
            foreach (var ill in IllegalReferences)
                hash = hash * 31 + ill.GetHashCode();
            foreach (var ci in ContractInterfaces)
                hash = hash * 31 + ci.GetHashCode();
            foreach (var cImpl in ContractImplementations)
                hash = hash * 31 + cImpl.GetHashCode();
            foreach (var pc in PermissionClasses)
                hash = hash * 31 + pc.GetHashCode();
            foreach (var ic in Interceptors)
                hash = hash * 31 + ic.GetHashCode();
            return hash;
        }
    }
}

internal readonly record struct ModuleInfoRecord(
    string FullyQualifiedName,
    string ModuleName,
    bool HasConfigureServices,
    bool HasConfigureEndpoints,
    bool HasConfigureMenu,
    bool HasConfigurePermissions,
    bool HasConfigureSettings,
    bool HasRazorComponents,
    string RoutePrefix,
    string ViewPrefix,
    ImmutableArray<EndpointInfoRecord> Endpoints,
    ImmutableArray<ViewInfoRecord> Views
)
{
    public bool Equals(ModuleInfoRecord other)
    {
        return FullyQualifiedName == other.FullyQualifiedName
            && ModuleName == other.ModuleName
            && HasConfigureServices == other.HasConfigureServices
            && HasConfigureEndpoints == other.HasConfigureEndpoints
            && HasConfigureMenu == other.HasConfigureMenu
            && HasConfigurePermissions == other.HasConfigurePermissions
            && HasConfigureSettings == other.HasConfigureSettings
            && HasRazorComponents == other.HasRazorComponents
            && RoutePrefix == other.RoutePrefix
            && ViewPrefix == other.ViewPrefix
            && Endpoints.SequenceEqual(other.Endpoints)
            && Views.SequenceEqual(other.Views);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + (ModuleName ?? "").GetHashCode();
            hash = hash * 31 + HasConfigureServices.GetHashCode();
            hash = hash * 31 + HasConfigureEndpoints.GetHashCode();
            hash = hash * 31 + HasConfigureMenu.GetHashCode();
            hash = hash * 31 + HasConfigurePermissions.GetHashCode();
            hash = hash * 31 + HasConfigureSettings.GetHashCode();
            hash = hash * 31 + HasRazorComponents.GetHashCode();
            hash = hash * 31 + (RoutePrefix ?? "").GetHashCode();
            hash = hash * 31 + (ViewPrefix ?? "").GetHashCode();
            foreach (var e in Endpoints)
                hash = hash * 31 + e.GetHashCode();
            foreach (var v in Views)
                hash = hash * 31 + v.GetHashCode();
            return hash;
        }
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
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + AllowAnonymous.GetHashCode();
            foreach (var p in RequiredPermissions)
                hash = hash * 31 + p.GetHashCode();
            return hash;
        }
    }
}

internal readonly record struct ViewInfoRecord(string FullyQualifiedName, string Page);

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
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + SafeName.GetHashCode();
            foreach (var p in Properties)
                hash = hash * 31 + p.GetHashCode();
            return hash;
        }
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
    ImmutableArray<DbSetInfoRecord> DbSets
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
            && DbSets.SequenceEqual(other.DbSets);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + (ModuleName ?? "").GetHashCode();
            hash = hash * 31 + IsIdentityDbContext.GetHashCode();
            hash = hash * 31 + (IdentityUserTypeFqn ?? "").GetHashCode();
            hash = hash * 31 + (IdentityRoleTypeFqn ?? "").GetHashCode();
            hash = hash * 31 + (IdentityKeyTypeFqn ?? "").GetHashCode();
            foreach (var d in DbSets)
                hash = hash * 31 + d.GetHashCode();
            return hash;
        }
    }
}

internal readonly record struct DbSetInfoRecord(string PropertyName, string EntityFqn);

internal readonly record struct EntityConfigInfoRecord(
    string ConfigFqn,
    string EntityFqn,
    string ModuleName
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
    string ReferencedAssemblyName
);

internal readonly record struct ContractInterfaceInfoRecord(
    string ContractsAssemblyName,
    string InterfaceName,
    int MethodCount
);

internal readonly record struct ContractImplementationRecord(
    string InterfaceFqn,
    string ImplementationFqn,
    string ModuleName,
    bool IsPublic,
    bool IsAbstract,
    bool DependsOnDbContext
);

internal readonly record struct PermissionClassRecord(
    string FullyQualifiedName,
    string ModuleName,
    bool IsSealed,
    ImmutableArray<PermissionFieldRecord> Fields
)
{
    public bool Equals(PermissionClassRecord other) =>
        FullyQualifiedName == other.FullyQualifiedName
        && ModuleName == other.ModuleName
        && IsSealed == other.IsSealed
        && Fields.SequenceEqual(other.Fields);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + (ModuleName ?? "").GetHashCode();
            hash = hash * 31 + IsSealed.GetHashCode();
            foreach (var f in Fields)
                hash = hash * 31 + f.GetHashCode();
            return hash;
        }
    }
}

internal readonly record struct PermissionFieldRecord(
    string FieldName,
    string Value,
    bool IsConstString
);

internal readonly record struct InterceptorInfoRecord(
    string FullyQualifiedName,
    string ModuleName,
    ImmutableArray<string> ConstructorParamTypeFqns
)
{
    public bool Equals(InterceptorInfoRecord other) =>
        FullyQualifiedName == other.FullyQualifiedName
        && ModuleName == other.ModuleName
        && ConstructorParamTypeFqns.SequenceEqual(other.ConstructorParamTypeFqns);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + (ModuleName ?? "").GetHashCode();
            foreach (var p in ConstructorParamTypeFqns)
                hash = hash * 31 + p.GetHashCode();
            return hash;
        }
    }
}

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
    public bool HasConfigureSettings { get; set; }
    public bool HasRazorComponents { get; set; }
    public string RoutePrefix { get; set; } = "";
    public string ViewPrefix { get; set; } = "";
    public List<EndpointInfo> Endpoints { get; set; } = new();
    public List<ViewInfo> Views { get; set; } = new();
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
    public string Page { get; set; } = "";
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
}

internal sealed class ContractImplementationInfo
{
    public string InterfaceFqn { get; set; } = "";
    public string ImplementationFqn { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsAbstract { get; set; }
    public bool DependsOnDbContext { get; set; }
}

internal sealed class PermissionClassInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsSealed { get; set; }
    public List<PermissionFieldInfo> Fields { get; set; } = new();
}

internal sealed class PermissionFieldInfo
{
    public string FieldName { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsConstString { get; set; }
}

internal sealed class InterceptorInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public List<string> ConstructorParamTypeFqns { get; set; } = new();
}

#endregion
