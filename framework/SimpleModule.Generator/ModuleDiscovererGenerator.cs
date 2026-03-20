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
        new RazorComponentExtensionsEmitter(),
        new ViewPagesEmitter(),
        new PageRegistryEmitter(),
        new JsonResolverEmitter(),
        new TypeScriptDefinitionsEmitter(),
        new HostDbContextEmitter(),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Extract an equatable data model from the compilation so the incremental
        // pipeline can cache results and skip re-generation when nothing changes.
        var dataProvider = context.CompilationProvider.Select(
            static (compilation, _) => SymbolDiscovery.Extract(compilation)
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
