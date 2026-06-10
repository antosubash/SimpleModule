using System.Collections.Generic;

namespace SimpleModule.Core.Modules;

/// <summary>
/// Compile-time metadata describing a module: identity, framework compatibility,
/// declared permissions, frontend entry asset, and the domain events it publishes
/// and consumes. Emitted into each module assembly by SimpleModule.Generator as a
/// <see cref="ModuleManifestAttribute"/> and read back via
/// <see cref="ModuleManifestReader"/>.
/// </summary>
public sealed class ModuleManifest
{
    /// <summary>Manifest schema version this assembly was compiled against.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Package/assembly identity, e.g. <c>SimpleModule.FeatureFlags</c>.</summary>
    public string Id { get; init; } = "";

    /// <summary>Module name from the <c>[Module]</c> attribute, e.g. <c>FeatureFlags</c>.</summary>
    public string Name { get; init; } = "";

    /// <summary>Human-readable name; defaults to <see cref="Name"/> when not customized.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Module version from the <c>[Module]</c> attribute.</summary>
    public string Version { get; init; } = "";

    /// <summary>
    /// SemVer range of SimpleModule.Core versions this module was built for,
    /// e.g. <c>&gt;=0.0.38 &lt;1.0.0</c>.
    /// </summary>
    public string FrameworkCompat { get; init; } = "";

    /// <summary>API route prefix, e.g. <c>/api/feature-flags</c>.</summary>
    public string RoutePrefix { get; init; } = "";

    /// <summary>View route prefix, e.g. <c>/feature-flags</c>.</summary>
    public string ViewPrefix { get; init; } = "";

    /// <summary>
    /// Database schema/prefix name — the module name used as the
    /// <c>ModuleConnections</c> configuration key.
    /// </summary>
    public string Schema { get; init; } = "";

    /// <summary>Permission values declared by the module's permission classes.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>
    /// Static web asset path of the module's prebuilt frontend bundle relative to
    /// the web root (e.g. <c>_content/SimpleModule.X/SimpleModule.X.pages.js</c>),
    /// or <c>null</c> when the module ships no frontend pages.
    /// </summary>
    public string? FrontendEntry { get; init; }

    /// <summary>Inertia page names served by the module, e.g. <c>X/Browse</c>.</summary>
    public IReadOnlyList<string> Pages { get; init; } = [];

    /// <summary>Fully-qualified names of DomainEvent types declared by the module.</summary>
    public IReadOnlyList<string> EventsPublished { get; init; } = [];

    /// <summary>Fully-qualified names of DomainEvent types handled by the module.</summary>
    public IReadOnlyList<string> EventsConsumed { get; init; } = [];

    /// <summary>Whether the module owns its own DbContext.</summary>
    public bool HasDbContext { get; init; }
}
