using System.Collections.Generic;
using System.Collections.Immutable;

namespace SimpleModule.Generator;

public partial class ModuleDiscovererGenerator
{
    #region Equatable data model for incremental caching

    // These record types implement value equality so the incremental generator
    // pipeline can detect when the extracted data hasn't changed and skip
    // re-generating source files.

    private readonly record struct DiscoveryData(
        ImmutableArray<ModuleInfoRecord> Modules,
        ImmutableArray<DtoTypeInfoRecord> DtoTypes,
        ImmutableArray<DbContextInfoRecord> DbContexts,
        ImmutableArray<EntityConfigInfoRecord> EntityConfigs
    )
    {
        public static readonly DiscoveryData Empty = new(
            ImmutableArray<ModuleInfoRecord>.Empty,
            ImmutableArray<DtoTypeInfoRecord>.Empty,
            ImmutableArray<DbContextInfoRecord>.Empty,
            ImmutableArray<EntityConfigInfoRecord>.Empty
        );

        public bool Equals(DiscoveryData other)
        {
            return Modules.SequenceEqual(other.Modules)
                && DtoTypes.SequenceEqual(other.DtoTypes)
                && DbContexts.SequenceEqual(other.DbContexts)
                && EntityConfigs.SequenceEqual(other.EntityConfigs);
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
                return hash;
            }
        }
    }

    private readonly record struct ModuleInfoRecord(
        string FullyQualifiedName,
        string ModuleName,
        bool HasConfigureServices,
        bool HasConfigureEndpoints,
        bool HasConfigureMenu,
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

    private readonly record struct EndpointInfoRecord(string FullyQualifiedName);

    private readonly record struct ViewInfoRecord(string FullyQualifiedName, string Page);

    private readonly record struct DtoTypeInfoRecord(
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

    private readonly record struct DtoPropertyInfoRecord(
        string Name,
        string TypeFqn,
        bool HasSetter
    );

    private readonly record struct DbContextInfoRecord(
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

    private readonly record struct DbSetInfoRecord(string PropertyName, string EntityFqn);

    private readonly record struct EntityConfigInfoRecord(
        string ConfigFqn,
        string EntityFqn,
        string ModuleName
    );

    #endregion

    #region Mutable working types (used during symbol traversal only)

    private sealed class ModuleInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public bool HasConfigureServices { get; set; }
        public bool HasConfigureEndpoints { get; set; }
        public bool HasConfigureMenu { get; set; }
        public bool HasRazorComponents { get; set; }
        public string RoutePrefix { get; set; } = "";
        public string ViewPrefix { get; set; } = "";
        public List<EndpointInfo> Endpoints { get; set; } = new();
        public List<ViewInfo> Views { get; set; } = new();
    }

    private sealed class EndpointInfo
    {
        public string FullyQualifiedName { get; set; } = "";
    }

    private sealed class ViewInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public string Page { get; set; } = "";
    }

    private sealed class DtoTypeInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public string SafeName { get; set; } = "";
        public List<DtoPropertyInfo> Properties { get; set; } = new();
    }

    private sealed class DtoPropertyInfo
    {
        public string Name { get; set; } = "";
        public string TypeFqn { get; set; } = "";
        public bool HasSetter { get; set; }
    }

    private sealed class DbContextInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public bool IsIdentityDbContext { get; set; }
        public string IdentityUserTypeFqn { get; set; } = "";
        public string IdentityRoleTypeFqn { get; set; } = "";
        public string IdentityKeyTypeFqn { get; set; } = "";
        public List<DbSetInfo> DbSets { get; set; } = new();
    }

    private sealed class DbSetInfo
    {
        public string PropertyName { get; set; } = "";
        public string EntityFqn { get; set; } = "";
    }

    private sealed class EntityConfigInfo
    {
        public string ConfigFqn { get; set; } = "";
        public string EntityFqn { get; set; } = "";
        public string ModuleName { get; set; } = "";
    }

    #endregion
}
