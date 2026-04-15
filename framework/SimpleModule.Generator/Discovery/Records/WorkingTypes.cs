using System.Collections.Generic;

namespace SimpleModule.Generator;

// Mutable working types used only during symbol traversal.
// After discovery completes, these are projected into equatable XxxRecord types.

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
