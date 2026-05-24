using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor FormRequestNotSealed = new(
        id: "SM0056",
        title: "FormRequest class must be sealed",
        messageFormat: "FormRequest '{0}' is not sealed. FormRequest classes must be sealed to prevent inheritance hierarchies that break validation caching and make the pipeline unpredictable.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor FormRequestDoesNotExtendBase = new(
        id: "SM0057",
        title: "FormRequest class must extend FormRequest<TSelf>",
        messageFormat: "FormRequest '{0}' has the [FormRequest] attribute but does not extend FormRequest<{0}>. The class must extend FormRequest<TSelf> to participate in the validation pipeline.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
