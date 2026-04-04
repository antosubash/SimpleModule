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
        new AgentExtensionsEmitter(),
        new LocalizationExtensionsEmitter(),
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

        context.RegisterSourceOutput(
            dataProvider,
            static (spc, data) =>
            {
                if (data.Modules.Length == 0)
                    return;

                foreach (var emitter in Emitters)
                {
                    emitter.Emit(spc, data);
                }
            }
        );
    }
}
