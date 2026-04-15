using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor CircularModuleDependency = new(
        id: "SM0010",
        title: "Circular module dependency detected",
        messageFormat: "Circular module dependency detected. Cycle: {0}. {1}To break this cycle, identify which direction is the primary dependency and reverse the other using IEventBus. For example, if {2} is the primary consumer of {3}: (1) Keep {2} \u2192 {3}.Contracts. (2) Remove {3} \u2192 {2}.Contracts. (3) In {3}, publish events via IEventBus instead of calling {2} directly. (4) In {2}, implement IEventHandler<T> to handle those events. Learn more: https://docs.simplemodule.dev/module-dependencies.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor IllegalImplementationReference = new(
        id: "SM0011",
        title: "Module directly references another module's implementation",
        messageFormat: "Module '{0}' directly references module '{1}' implementation assembly '{2}'. Modules must only depend on each other through Contracts packages. This creates tight coupling \u2014 internal changes in {1} can break {0} at compile time or runtime. To fix: (1) Remove the reference to '{2}'. (2) Add a reference to '{1}.Contracts' instead. (3) Replace any usage of internal {1} types with their contract interfaces. Learn more: https://docs.simplemodule.dev/module-contracts.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
