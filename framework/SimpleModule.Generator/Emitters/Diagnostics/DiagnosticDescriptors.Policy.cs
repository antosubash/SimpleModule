using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor PolicyResourceMustBeDto = new(
        id: "SM0058",
        title: "Policy resource type must be a contracts DTO",
        messageFormat: "Policy '{0}' targets resource type '{1}' which is not a contracts DTO. Policies guard resources that cross module boundaries — move the resource type to the module's .Contracts assembly or mark it with [Dto].",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
