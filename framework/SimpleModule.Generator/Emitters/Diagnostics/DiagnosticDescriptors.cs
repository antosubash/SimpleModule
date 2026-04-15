using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor DuplicateDbSetPropertyName = new(
        id: "SM0001",
        title: "Duplicate DbSet property name across modules",
        messageFormat: "DbSet property name '{0}' is used by multiple modules: {1} (entity {2}) and {3} (entity {4}). Each module must use unique DbSet property names to avoid table name conflicts in the unified HostDbContext.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor EmptyModuleName = new(
        id: "SM0002",
        title: "Module has empty name",
        messageFormat: "Module class '{0}' has an empty [Module] name. Provide a non-empty name: [Module(\"MyModule\")]. An empty name will cause broken route prefixes, schema names, and TypeScript module grouping.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor MultipleIdentityDbContexts = new(
        id: "SM0003",
        title: "Multiple IdentityDbContext types found",
        messageFormat: "Multiple modules define an IdentityDbContext: '{0}' (module {1}) and '{2}' (module {3}). Only one module should provide Identity. The unified HostDbContext can only extend one IdentityDbContext base class.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor IdentityDbContextBadTypeArgs = new(
        id: "SM0005",
        title: "IdentityDbContext has unexpected type arguments",
        messageFormat: "IdentityDbContext '{0}' in module '{1}' must extend IdentityDbContext<TUser, TRole, TKey> with exactly 3 type arguments, but {2} were found. Use the 3-argument form: IdentityDbContext<ApplicationUser, ApplicationRole, string>.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor EntityConfigForMissingEntity = new(
        id: "SM0006",
        title: "Entity configuration targets entity not in any DbSet",
        messageFormat: "IEntityTypeConfiguration<{0}> in '{1}' (module '{2}') configures an entity that is not exposed as a DbSet in any module's DbContext. Add a DbSet<{0}> property to a DbContext, or remove this configuration.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor DuplicateEntityConfiguration = new(
        id: "SM0007",
        title: "Duplicate entity configuration",
        messageFormat: "Entity '{0}' has multiple IEntityTypeConfiguration implementations: '{1}' and '{2}'. EF Core only supports one configuration per entity type. Remove the duplicate.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

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

    internal static readonly DiagnosticDescriptor DuplicateViewPageName = new(
        id: "SM0015",
        title: "Duplicate view page name across modules",
        messageFormat: "View page name '{0}' is registered by multiple endpoints: '{1}' (module {2}) and '{3}' (module {4}). Each IViewEndpoint must map to a unique page name. Rename one of the endpoint classes or move it to a different module.",
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

    internal static readonly DiagnosticDescriptor PermissionFieldNotConstString = new(
        id: "SM0027",
        title: "Permission field is not a const string",
        messageFormat: "Permission class '{0}' must contain only public const string fields. Found field '{1}' that is not a const string.",
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

    internal static readonly DiagnosticDescriptor InterceptorDependsOnDbContext = new(
        id: "SM0039",
        title: "SaveChanges interceptor has transitive DbContext dependency",
        messageFormat: "ISaveChangesInterceptor '{0}' in module '{1}' has a constructor parameter '{2}' whose implementation depends on a DbContext. This creates a circular dependency when ModuleDbContextOptionsBuilder resolves interceptors from DI during DbContext options construction. To fix: make the parameter optional and resolve it lazily, or remove the dependency.",
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

    internal static readonly DiagnosticDescriptor EmptyModuleWarning = new(
        id: "SM0043",
        title: "Module does not override any IModule methods",
        messageFormat: "Module '{0}' implements IModule but does not override any configuration methods (ConfigureServices, ConfigureMenu, etc.). This module will be discovered but has no effect. If this is intentional, add at least ConfigureServices with a comment explaining why.",
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

    internal static readonly DiagnosticDescriptor EntityNotInContractsAssembly = new(
        id: "SM0055",
        title: "Entity class must live in a Contracts assembly",
        messageFormat: "Entity '{0}' is exposed as DbSet '{1}' on '{2}' but is declared in assembly '{3}'. Entity classes must be declared in a '.Contracts' assembly so other modules can reference them type-safely through contracts. Move '{0}' to assembly '{4}' (or another '.Contracts' assembly that the module references).",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
