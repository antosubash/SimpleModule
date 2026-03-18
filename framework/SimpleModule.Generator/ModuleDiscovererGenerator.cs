using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

[Generator]
public partial class ModuleDiscovererGenerator : IIncrementalGenerator
{
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

                new DiagnosticEmitter().Emit(spc, data);

                new ModuleExtensionsEmitter().Emit(spc, data);
                new EndpointExtensionsEmitter().Emit(spc, data);
                new MenuExtensionsEmitter().Emit(spc, data);
                new RazorComponentExtensionsEmitter().Emit(spc, data);
                GenerateViewPages(spc, data.Modules);

                if (data.DtoTypes.Length > 0)
                {
                    GenerateJsonResolver(spc, data.DtoTypes);
                    GenerateTypeScriptDefinitions(spc, data.DtoTypes);
                }

                if (data.DbContexts.Length > 0)
                {
                    EmitHostDbContext(spc, data);
                }
            }
        );
    }
}
