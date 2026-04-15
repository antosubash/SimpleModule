using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor EmptyModuleName = new(
        id: "SM0002",
        title: "Module has empty name",
        messageFormat: "Module class '{0}' has an empty [Module] name. Provide a non-empty name: [Module(\"MyModule\")]. An empty name will cause broken route prefixes, schema names, and TypeScript module grouping.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor DuplicateModuleName = new(
        id: "SM0040",
        title: "Duplicate module name",
        messageFormat: "Module name '{0}' is used by both '{1}' and '{2}'. Each module must have a unique name. Duplicate names cause route prefix conflicts, database schema collisions, and ambiguous TypeScript module grouping.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor EmptyModuleWarning = new(
        id: "SM0043",
        title: "Module does not override any IModule methods",
        messageFormat: "Module '{0}' implements IModule but does not override any configuration methods (ConfigureServices, ConfigureMenu, etc.). This module will be discovered but has no effect. If this is intentional, add at least ConfigureServices with a comment explaining why.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
