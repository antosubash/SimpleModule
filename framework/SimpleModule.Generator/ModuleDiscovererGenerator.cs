using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

[Generator]
public class ModuleDiscovererGenerator : IIncrementalGenerator
{
    private static readonly IEmitter[] Emitters =
    [
        new DiagnosticEmitter(),
        new ModuleExtensionsEmitter(),
        new EndpointExtensionsEmitter(),
        new MenuExtensionsEmitter(),
        new SettingsExtensionsEmitter(),
        new ViewPagesEmitter(),
        new PageRegistryEmitter(),
        new JsonResolverEmitter(),
        new TypeScriptDefinitionsEmitter(),
        new HostingExtensionsEmitter(),
        new ModuleOptionsEmitter(),
        new HostDbContextEmitter(),
        new ValueConverterConventionsEmitter(),
        new DbContextRegistryEmitter(),
        new ContractRegistryEmitter(),
        new AgentExtensionsEmitter(),
        new LocalizationExtensionsEmitter(),
        new RoutesEmitter(),
        new TypeScriptRoutesEmitter(),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Extract an equatable data model from the compilation so the incremental
        // pipeline can cache results and skip re-generation when nothing changes.
        // The CancellationToken allows the IDE to cancel stale discovery work
        // when a new compilation is triggered (e.g., on each keystroke).
        var dataProvider = context.CompilationProvider.Select(
            static (compilation, cancellationToken) =>
                SymbolDiscovery.Extract(compilation, cancellationToken)
        );

        // MSBuild properties surfaced via <CompilerVisibleProperty>:
        //   SimpleModuleProjectKind = "Module" switches the generator from host
        //   emission (AddModules, endpoint maps, ...) to emitting only the module
        //   manifest attribute into the module's own assembly.
        //   SimpleModuleFrameworkCompat overrides the manifest's compat range.
        var optionsProvider = context.AnalyzerConfigOptionsProvider.Select(
            static (provider, _) =>
            {
                provider.GlobalOptions.TryGetValue(
                    "build_property.SimpleModuleProjectKind",
                    out var kind
                );
                provider.GlobalOptions.TryGetValue(
                    "build_property.SimpleModuleFrameworkCompat",
                    out var compat
                );
                return (Kind: kind ?? "", Compat: compat ?? "");
            }
        );

        context.RegisterSourceOutput(
            dataProvider.Combine(optionsProvider),
            static (spc, pair) =>
            {
                var (data, options) = pair;
                if (data.Modules.Length == 0)
                    return;

                if (
                    string.Equals(options.Kind, "Module", System.StringComparison.OrdinalIgnoreCase)
                )
                {
                    ModuleManifestEmitter.Emit(spc, data, options.Compat);
                    return;
                }

                foreach (var emitter in Emitters)
                {
                    emitter.Emit(spc, data);
                }
            }
        );
    }
}
