using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

/// <summary>
/// Pre-resolved Roslyn type symbols needed during discovery. Resolving each
/// symbol once via <see cref="Compilation.GetTypeByMetadataName"/> at the top
/// of <c>SymbolDiscovery.Extract</c> is dramatically cheaper than scattering
/// calls across finder methods — every call force-resolves the namespace
/// chain, so caching them saves ~15 lookups per Extract invocation.
/// </summary>
internal readonly record struct CoreSymbols(
    INamedTypeSymbol ModuleAttribute,
    INamedTypeSymbol? DtoAttribute,
    INamedTypeSymbol? EndpointInterface,
    INamedTypeSymbol? ViewEndpointInterface,
    INamedTypeSymbol? AgentDefinition,
    INamedTypeSymbol? AgentToolProvider,
    INamedTypeSymbol? KnowledgeSource,
    INamedTypeSymbol? ModuleServices,
    INamedTypeSymbol? ModuleMenu,
    INamedTypeSymbol? ModuleMiddleware,
    INamedTypeSymbol? ModuleSettings,
    INamedTypeSymbol? NoDtoAttribute,
    INamedTypeSymbol? EventInterface,
    INamedTypeSymbol? ModulePermissions,
    INamedTypeSymbol? ModuleFeatures,
    INamedTypeSymbol? SaveChangesInterceptor,
    INamedTypeSymbol? ModuleOptions,
    bool HasAgentsAssembly,
    bool HasRagAssembly
)
{
    /// <summary>
    /// Resolves all framework type symbols from the current compilation.
    /// Returns null if the ModuleAttribute itself isn't resolvable —
    /// discovery cannot proceed without it.
    /// </summary>
    internal static CoreSymbols? TryResolve(Compilation compilation)
    {
        var moduleAttribute = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.ModuleAttribute"
        );
        if (moduleAttribute is null)
            return null;

        return new CoreSymbols(
            ModuleAttribute: moduleAttribute,
            DtoAttribute: compilation.GetTypeByMetadataName("SimpleModule.Core.DtoAttribute"),
            EndpointInterface: compilation.GetTypeByMetadataName("SimpleModule.Core.IEndpoint"),
            ViewEndpointInterface: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.IViewEndpoint"
            ),
            AgentDefinition: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.Agents.IAgentDefinition"
            ),
            AgentToolProvider: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.Agents.IAgentToolProvider"
            ),
            KnowledgeSource: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.Rag.IKnowledgeSource"
            ),
            ModuleServices: compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleServices"),
            ModuleMenu: compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleMenu"),
            ModuleMiddleware: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.IModuleMiddleware"
            ),
            ModuleSettings: compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleSettings"),
            NoDtoAttribute: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.NoDtoGenerationAttribute"
            ),
            EventInterface: compilation.GetTypeByMetadataName("SimpleModule.Core.Events.IEvent"),
            ModulePermissions: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.Authorization.IModulePermissions"
            ),
            ModuleFeatures: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.FeatureFlags.IModuleFeatures"
            ),
            SaveChangesInterceptor: compilation.GetTypeByMetadataName(
                "Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor"
            ),
            ModuleOptions: compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleOptions"),
            HasAgentsAssembly: compilation.GetTypeByMetadataName(
                "SimpleModule.Agents.SimpleModuleAgentExtensions"
            )
                is not null,
            HasRagAssembly: compilation.GetTypeByMetadataName(
                "SimpleModule.Rag.RagSettingsDefinitions"
            )
                is not null
        );
    }
}
