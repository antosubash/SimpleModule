using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor DuplicateViewPageName = new(
        id: "SM0015",
        title: "Duplicate view page name across modules",
        messageFormat: "View page name '{0}' is registered by multiple endpoints: '{1}' (module {2}) and '{3}' (module {4}). Each IViewEndpoint must map to a unique page name. Rename one of the endpoint classes or move it to a different module.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor ViewPagePrefixMismatch = new(
        id: "SM0041",
        title: "View page name does not match module name prefix",
        messageFormat: "View endpoint '{0}' in module '{1}' maps to page '{2}', but page names should start with the module name prefix '{1}/'. This causes the React page resolver to look for the page bundle in the wrong module. Rename the endpoint class or move it to the correct module.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor ViewEndpointWithoutViewPrefix = new(
        id: "SM0042",
        title: "Module has view endpoints but no ViewPrefix",
        messageFormat: "Module '{0}' contains {1} IViewEndpoint implementation(s) but does not define a ViewPrefix. View endpoints will not be routed correctly. Add ViewPrefix to the [Module] attribute: [Module(\"{0}\", ViewPrefix = \"/{2}\")].",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor InterceptorDependsOnDbContext = new(
        id: "SM0039",
        title: "SaveChanges interceptor has transitive DbContext dependency",
        messageFormat: "ISaveChangesInterceptor '{0}' in module '{1}' has a constructor parameter '{2}' whose implementation depends on a DbContext. This creates a circular dependency when ModuleDbContextOptionsBuilder resolves interceptors from DI during DbContext options construction. To fix: make the parameter optional and resolve it lazily, or remove the dependency.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor MultipleEndpointsPerFile = new(
        id: "SM0049",
        title: "Multiple endpoints in a single file",
        messageFormat: "File '{0}' contains multiple endpoint classes ({1}). Each endpoint must be in its own file for maintainability and to match the Pages/index.ts convention.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor ModuleAssemblyNamingViolation = new(
        id: "SM0052",
        title: "Module assembly name does not follow naming convention",
        messageFormat: "Module '{0}' is in assembly '{1}', but the assembly name must be 'SimpleModule.{0}' (or 'SimpleModule.{0}.Module' when a framework assembly with the same base name exists). Rename the project/assembly to follow the standard module naming convention.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor MissingContractsAssembly = new(
        id: "SM0053",
        title: "Module has no matching Contracts assembly",
        messageFormat: "Module '{0}' (assembly '{1}') has no matching Contracts assembly. Every module must have a 'SimpleModule.{0}.Contracts' project with at least one public interface. Create the project and add a reference to it.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor MissingEndpointRouteConst = new(
        id: "SM0054",
        title: "Endpoint missing Route const field",
        messageFormat: "Endpoint '{0}' does not declare a 'public const string Route' field. Add a Route const so the source generator can emit type-safe route helpers.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );
}
