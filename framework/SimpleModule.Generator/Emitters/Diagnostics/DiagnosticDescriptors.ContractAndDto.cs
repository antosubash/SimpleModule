using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor ContractInterfaceTooLargeWarning = new(
        id: "SM0012",
        title: "Contract interface has too many methods",
        messageFormat: "Contract interface '{0}' has {1} methods, which exceeds the recommended maximum of 15. Large contract interfaces force consuming modules to depend on methods they don't use. Consider splitting into focused interfaces (e.g., I{2}Queries, I{2}Commands). Your module class can implement all of them. Warning threshold: 15 methods, error threshold: 20 methods. Learn more: https://docs.simplemodule.dev/contract-design.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor ContractInterfaceTooLargeError = new(
        id: "SM0013",
        title: "Contract interface must be split",
        messageFormat: "Contract interface '{0}' has {1} methods and must be split before the project will compile. Interfaces with more than 20 methods are not allowed. Split into focused interfaces (e.g., I{2}Queries, I{2}Commands). Your module class can implement all of them. Warning threshold: 15 methods, error threshold: 20 methods. Learn more: https://docs.simplemodule.dev/contract-design.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor MissingContractInterfaces = new(
        id: "SM0014",
        title: "Referenced contracts assembly has no public interfaces",
        messageFormat: "Module '{0}' references '{1}' but no contract interfaces were found in that assembly. Likely causes: (1) Incompatible package version \u2014 check with 'dotnet list package --include-transitive'. (2) The Contracts project is empty or not yet built. (3) The package is corrupted \u2014 try 'dotnet nuget locals all --clear' then 'dotnet restore'. Verify that the version of {1} you're using exports the interfaces your code depends on. Learn more: https://docs.simplemodule.dev/package-compatibility.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor NoContractImplementation = new(
        id: "SM0025",
        title: "No implementation found for contract interface",
        messageFormat: "No implementation of '{0}' found in module '{1}'. Add a public class implementing this interface.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor MultipleContractImplementations = new(
        id: "SM0026",
        title: "Multiple implementations of contract interface",
        messageFormat: "Multiple implementations of '{0}' found in module '{1}': {2}. Only one implementation per contract interface is allowed.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor ContractImplementationNotPublic = new(
        id: "SM0028",
        title: "Contract implementation is not public",
        messageFormat: "Implementation '{0}' of '{1}' must be public. The DI container cannot access internal types across assemblies.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor ContractImplementationIsAbstract = new(
        id: "SM0029",
        title: "Contract implementation is abstract",
        messageFormat: "'{0}' implements '{1}' but is abstract. Provide a concrete implementation.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor DtoTypeNoProperties = new(
        id: "SM0035",
        title: "DTO type in contracts has no public properties",
        messageFormat: "'{0}' in '{1}' has no public properties. If this is not a DTO, mark it with [NoDtoGeneration].",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor InfrastructureTypeInContracts = new(
        id: "SM0038",
        title: "Infrastructure type in Contracts assembly",
        messageFormat: "'{0}' appears to be an infrastructure type in a Contracts assembly. Infrastructure types should not be in Contracts assemblies. Mark it with [NoDtoGeneration] or move it.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
