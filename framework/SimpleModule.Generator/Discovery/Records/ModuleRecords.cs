using System.Collections.Immutable;
using System.Linq;

namespace SimpleModule.Generator;

internal readonly record struct ModuleInfoRecord(
    string FullyQualifiedName,
    string ModuleName,
    string AssemblyName,
    bool HasConfigureServices,
    bool HasConfigureEndpoints,
    bool HasConfigureMenu,
    bool HasConfigurePermissions,
    bool HasConfigureMiddleware,
    bool HasConfigureSettings,
    bool HasConfigureFeatureFlags,
    bool HasConfigureAgents,
    bool HasConfigureRateLimits,
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
            && AssemblyName == other.AssemblyName
            && HasConfigureServices == other.HasConfigureServices
            && HasConfigureEndpoints == other.HasConfigureEndpoints
            && HasConfigureMenu == other.HasConfigureMenu
            && HasConfigureMiddleware == other.HasConfigureMiddleware
            && HasConfigurePermissions == other.HasConfigurePermissions
            && HasConfigureSettings == other.HasConfigureSettings
            && HasConfigureFeatureFlags == other.HasConfigureFeatureFlags
            && HasConfigureAgents == other.HasConfigureAgents
            && HasConfigureRateLimits == other.HasConfigureRateLimits
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
        hash = HashHelper.Combine(hash, (AssemblyName ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureServices.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureEndpoints.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureMenu.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureMiddleware.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigurePermissions.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureSettings.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureFeatureFlags.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureAgents.GetHashCode());
        hash = HashHelper.Combine(hash, HasConfigureRateLimits.GetHashCode());
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
    bool AllowAnonymous,
    string RouteTemplate,
    string HttpMethod
)
{
    public bool Equals(EndpointInfoRecord other)
    {
        return FullyQualifiedName == other.FullyQualifiedName
            && AllowAnonymous == other.AllowAnonymous
            && RouteTemplate == other.RouteTemplate
            && HttpMethod == other.HttpMethod
            && RequiredPermissions.SequenceEqual(other.RequiredPermissions);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = HashHelper.Combine(hash, FullyQualifiedName.GetHashCode());
        hash = HashHelper.Combine(hash, AllowAnonymous.GetHashCode());
        hash = HashHelper.Combine(hash, (RouteTemplate ?? "").GetHashCode());
        hash = HashHelper.Combine(hash, (HttpMethod ?? "").GetHashCode());
        hash = HashHelper.HashArray(hash, RequiredPermissions);
        return hash;
    }
}

internal readonly record struct ViewInfoRecord(
    string FullyQualifiedName,
    string Page,
    string RouteTemplate,
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
