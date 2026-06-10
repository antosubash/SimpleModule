using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor PolicyResourceMustBeDto = new(
        id: "SM0058",
        title: "Policy resource type must be a contracts DTO",
        messageFormat: "Policy '{0}' targets resource type '{1}' which is neither marked [Dto] nor declared in a .Contracts assembly. Policies guard resources that cross module boundaries — move the resource type to the module's .Contracts assembly or mark it with [Dto].",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor PolicyNotPublic = new(
        id: "SM0059",
        title: "Policy class must be public",
        messageFormat: "Policy '{0}' implements IPolicy<{1}> but is not public, so it cannot be auto-registered. Make the class public — otherwise authorization checks for '{1}' fail at runtime with MissingPolicyException.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor PolicyForForeignResource = new(
        id: "SM0060",
        title: "Policy must be owned by the resource's module",
        messageFormat: "Policy '{0}' in module '{1}' targets resource type '{2}' owned by module '{3}'. Policies run with deny-wins semantics, so a foreign policy would silently veto another module's authorization decisions — move the policy to module '{3}'.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor PolicyMustNotBeGeneric = new(
        id: "SM0061",
        title: "Policy class must not be generic",
        messageFormat: "Policy '{0}' is a generic class. Open generic policies cannot be auto-registered — declare one closed policy class per resource type instead.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
