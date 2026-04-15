using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor PermissionFieldNotConstString = new(
        id: "SM0027",
        title: "Permission field is not a const string",
        messageFormat: "Permission class '{0}' must contain only public const string fields. Found field '{1}' that is not a const string.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor PermissionValueBadPattern = new(
        id: "SM0031",
        title: "Permission value does not follow naming pattern",
        messageFormat: "Permission value '{0}' in '{1}' should follow the 'Module.Action' pattern, for example 'Products.View'",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor PermissionClassNotSealed = new(
        id: "SM0032",
        title: "Permission class is not sealed",
        messageFormat: "'{0}' implements IModulePermissions but is not sealed. Permission classes must be sealed.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor DuplicatePermissionValue = new(
        id: "SM0033",
        title: "Duplicate permission value",
        messageFormat: "Permission value '{0}' is defined in both '{1}' and '{2}'. Each permission value must be unique.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor PermissionValueWrongPrefix = new(
        id: "SM0034",
        title: "Permission value prefix does not match module name",
        messageFormat: "Permission '{0}' is defined in module '{1}'. Permission values should be prefixed with the owning module name.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor MultipleModuleOptions = new(
        id: "SM0044",
        title: "Multiple IModuleOptions for same module",
        messageFormat: "Module '{0}' has multiple IModuleOptions implementations: '{1}' and '{2}'. Each module should have at most one options class. Only the first will be used.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor FeatureClassNotSealed = new(
        id: "SM0045",
        title: "Feature class is not sealed",
        messageFormat: "'{0}' implements IModuleFeatures but is not sealed",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor FeatureFieldNamingViolation = new(
        id: "SM0046",
        title: "Feature field naming violation",
        messageFormat: "Feature '{0}' in '{1}' does not follow the 'ModuleName.FeatureName' pattern",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor DuplicateFeatureName = new(
        id: "SM0047",
        title: "Duplicate feature name",
        messageFormat: "Feature name '{0}' is defined in both '{1}' and '{2}'",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor FeatureFieldNotConstString = new(
        id: "SM0048",
        title: "Feature field is not a const string",
        messageFormat: "Field '{0}' in feature class '{1}' must be a public const string",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
