using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal sealed class DiagnosticEmitter : IEmitter
{
    internal static readonly DiagnosticDescriptor DuplicateDbSetPropertyName = new(
        id: "SM0001",
        title: "Duplicate DbSet property name across modules",
        messageFormat: "DbSet property name '{0}' is used by multiple modules: {1} (entity {2}) and {3} (entity {4}). Each module must use unique DbSet property names to avoid table name conflicts in the unified HostDbContext.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor EmptyModuleName = new(
        id: "SM0002",
        title: "Module has empty name",
        messageFormat: "Module class '{0}' has an empty [Module] name. Provide a non-empty name: [Module(\"MyModule\")]. An empty name will cause broken route prefixes, schema names, and TypeScript module grouping.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor MultipleIdentityDbContexts = new(
        id: "SM0003",
        title: "Multiple IdentityDbContext types found",
        messageFormat: "Multiple modules define an IdentityDbContext: '{0}' (module {1}) and '{2}' (module {3}). Only one module should provide Identity. The unified HostDbContext can only extend one IdentityDbContext base class.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor IdentityDbContextBadTypeArgs = new(
        id: "SM0005",
        title: "IdentityDbContext has unexpected type arguments",
        messageFormat: "IdentityDbContext '{0}' in module '{1}' must extend IdentityDbContext<TUser, TRole, TKey> with exactly 3 type arguments, but {2} were found. Use the 3-argument form: IdentityDbContext<ApplicationUser, ApplicationRole, string>.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor EntityConfigForMissingEntity = new(
        id: "SM0006",
        title: "Entity configuration targets entity not in any DbSet",
        messageFormat: "IEntityTypeConfiguration<{0}> in '{1}' (module '{2}') configures an entity that is not exposed as a DbSet in any module's DbContext. Add a DbSet<{0}> property to a DbContext, or remove this configuration.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor DuplicateEntityConfiguration = new(
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
        messageFormat: "Contract interface '{0}' has {1} methods, which exceeds the recommended maximum of 15. Large contract interfaces force consuming modules to depend on methods they don't use. Consider splitting into focused interfaces (e.g., I{2}Queries, I{2}Commands). Your module class can implement all of them. Thresholds are configurable in .editorconfig: simplemodule.max_contract_methods_warn = 15, simplemodule.max_contract_methods_error = 20. Learn more: https://docs.simplemodule.dev/contract-design.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor ContractInterfaceTooLargeError = new(
        id: "SM0013",
        title: "Contract interface must be split",
        messageFormat: "Contract interface '{0}' has {1} methods and must be split before the project will compile. Interfaces with more than 20 methods are not allowed. Split into focused interfaces (e.g., I{2}Queries, I{2}Commands). Your module class can implement all of them. Thresholds are configurable in .editorconfig: simplemodule.max_contract_methods_warn = 15, simplemodule.max_contract_methods_error = 20. Learn more: https://docs.simplemodule.dev/contract-design.",
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

    private static readonly DiagnosticDescriptor NoContractImplementation = new(
        id: "SM0025",
        title: "No implementation found for contract interface",
        messageFormat: "No implementation of '{0}' found in module '{1}'. Add a public class implementing this interface.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor MultipleContractImplementations = new(
        id: "SM0026",
        title: "Multiple implementations of contract interface",
        messageFormat: "Multiple implementations of '{0}' found in module '{1}': {2}. Only one implementation per contract interface is allowed.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor PermissionFieldNotConstString = new(
        id: "SM0027",
        title: "Permission field is not a const string",
        messageFormat: "Permission class '{0}' must contain only public const string fields. Found field '{1}' that is not a const string.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor ContractImplementationNotPublic = new(
        id: "SM0028",
        title: "Contract implementation is not public",
        messageFormat: "Implementation '{0}' of '{1}' must be public. The DI container cannot access internal types across assemblies.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor ContractImplementationIsAbstract = new(
        id: "SM0029",
        title: "Contract implementation is abstract",
        messageFormat: "'{0}' implements '{1}' but is abstract. Provide a concrete implementation.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor PermissionValueBadPattern = new(
        id: "SM0031",
        title: "Permission value does not follow naming pattern",
        messageFormat: "Permission value '{0}' in '{1}' should follow the 'Module.Action' pattern, for example 'Products.View'",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor PermissionClassNotSealed = new(
        id: "SM0032",
        title: "Permission class is not sealed",
        messageFormat: "'{0}' implements IModulePermissions but is not sealed. Permission classes must be sealed.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor DuplicatePermissionValue = new(
        id: "SM0033",
        title: "Duplicate permission value",
        messageFormat: "Permission value '{0}' is defined in both '{1}' and '{2}'. Each permission value must be unique.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor PermissionValueWrongPrefix = new(
        id: "SM0034",
        title: "Permission value prefix does not match module name",
        messageFormat: "Permission '{0}' is defined in module '{1}'. Permission values should be prefixed with the owning module name.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor DtoTypeNoProperties = new(
        id: "SM0035",
        title: "DTO type in contracts has no public properties",
        messageFormat: "'{0}' in '{1}' has no public properties. If this is not a DTO, mark it with [NoDtoGeneration].",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor InfrastructureTypeInContracts = new(
        id: "SM0038",
        title: "Infrastructure type in Contracts assembly",
        messageFormat: "'{0}' appears to be an infrastructure type in a Contracts assembly. Infrastructure types should not be in Contracts assemblies. Mark it with [NoDtoGeneration] or move it.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
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

    public void Emit(SourceProductionContext context, DiscoveryData data)
    {
        // SM0002: Empty module name
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        EmptyModuleName,
                        Location.None,
                        Strip(module.FullyQualifiedName)
                    )
                );
            }
        }

        // SM0040: Duplicate module name
        var seenModuleNames = new Dictionary<string, string>();
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
                continue;

            if (seenModuleNames.TryGetValue(module.ModuleName, out var existingFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DuplicateModuleName,
                        Location.None,
                        module.ModuleName,
                        Strip(existingFqn),
                        Strip(module.FullyQualifiedName)
                    )
                );
            }
            else
            {
                seenModuleNames[module.ModuleName] = module.FullyQualifiedName;
            }
        }

        // SM0004: DbContext with no DbSets — silently skipped.
        // Some DbContexts (e.g., OpenIddict) manage tables internally without public DbSet<T>
        // properties. These are excluded from the unified HostDbContext but are not an error.

        // SM0003: Multiple IdentityDbContexts
        DbContextInfoRecord? firstIdentity = null;
        foreach (var ctx in data.DbContexts)
        {
            if (!ctx.IsIdentityDbContext)
                continue;

            if (firstIdentity is null)
            {
                firstIdentity = ctx;
            }
            else
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MultipleIdentityDbContexts,
                        Location.None,
                        Strip(firstIdentity.Value.FullyQualifiedName),
                        firstIdentity.Value.ModuleName,
                        Strip(ctx.FullyQualifiedName),
                        ctx.ModuleName
                    )
                );
            }
        }

        // SM0005: IdentityDbContext with wrong type args
        foreach (var ctx in data.DbContexts)
        {
            if (ctx.IsIdentityDbContext && string.IsNullOrEmpty(ctx.IdentityUserTypeFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        IdentityDbContextBadTypeArgs,
                        Location.None,
                        Strip(ctx.FullyQualifiedName),
                        ctx.ModuleName,
                        0
                    )
                );
            }
        }

        // SM0006: Entity config for entity not in any DbSet
        var allEntityFqns = new HashSet<string>();
        foreach (var ctx in data.DbContexts)
        {
            foreach (var dbSet in ctx.DbSets)
                allEntityFqns.Add(dbSet.EntityFqn);
        }

        foreach (var config in data.EntityConfigs)
        {
            if (!allEntityFqns.Contains(config.EntityFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        EntityConfigForMissingEntity,
                        Location.None,
                        Strip(config.EntityFqn),
                        Strip(config.ConfigFqn),
                        config.ModuleName
                    )
                );
            }
        }

        // SM0007: Duplicate EntityTypeConfiguration for same entity
        var entityConfigOwners = new Dictionary<string, string>();
        foreach (var config in data.EntityConfigs)
        {
            if (entityConfigOwners.TryGetValue(config.EntityFqn, out var existing))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DuplicateEntityConfiguration,
                        Location.None,
                        Strip(config.EntityFqn),
                        existing,
                        Strip(config.ConfigFqn)
                    )
                );
            }
            else
            {
                entityConfigOwners[config.EntityFqn] = Strip(config.ConfigFqn);
            }
        }

        // SM0010: Circular module dependency
        var (_, sortResult) = TopologicalSort.SortModulesWithResult(data);

        if (!sortResult.IsSuccess && sortResult.Cycle.Length > 0)
        {
            // Build cycle string: "A → B → C → A"
            var cycleNodes = new List<string>();
            foreach (var c in sortResult.Cycle)
                cycleNodes.Add(c);
            cycleNodes.Add(sortResult.Cycle[0]); // close the loop
            var cycleStr = string.Join(" \u2192 ", cycleNodes);

            // Build "how it happened" string
            var cycleSet = new HashSet<string>();
            foreach (var c in sortResult.Cycle)
                cycleSet.Add(c);

            var howParts = new List<string>();
            foreach (var dep in data.Dependencies)
            {
                if (cycleSet.Contains(dep.ModuleName) && cycleSet.Contains(dep.DependsOnModuleName))
                {
                    howParts.Add(
                        dep.ModuleName + " references " + dep.ContractsAssemblyName + ". "
                    );
                }
            }
            var howStr = string.Join("", howParts);

            var first = sortResult.Cycle[0];
            var second = sortResult.Cycle.Length > 1 ? sortResult.Cycle[1] : first;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    CircularModuleDependency,
                    Location.None,
                    cycleStr,
                    howStr,
                    first,
                    second
                )
            );
        }

        // SM0011: Illegal implementation references
        foreach (var illegal in data.IllegalReferences)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    IllegalImplementationReference,
                    Location.None,
                    illegal.ReferencingModuleName,
                    illegal.ReferencedModuleName,
                    illegal.ReferencedAssemblyName
                )
            );
        }

        // SM0012/SM0013: Contract interface size
        foreach (var iface in data.ContractInterfaces)
        {
            if (iface.MethodCount > 20)
            {
                var shortName = ExtractShortName(iface.InterfaceName);
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ContractInterfaceTooLargeError,
                        Location.None,
                        Strip(iface.InterfaceName),
                        iface.MethodCount,
                        shortName
                    )
                );
            }
            else if (iface.MethodCount >= 15)
            {
                var shortName = ExtractShortName(iface.InterfaceName);
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ContractInterfaceTooLargeWarning,
                        Location.None,
                        Strip(iface.InterfaceName),
                        iface.MethodCount,
                        shortName
                    )
                );
            }
        }

        // SM0014: Missing contract interfaces in referenced contracts assemblies
        var contractsWithInterfaces = new HashSet<string>();
        foreach (var iface in data.ContractInterfaces)
            contractsWithInterfaces.Add(iface.ContractsAssemblyName);

        // Deduplicate: only report once per (module, contracts assembly) pair
        var reported = new HashSet<string>();
        foreach (var dep in data.Dependencies)
        {
            var key = dep.ModuleName + "|" + dep.ContractsAssemblyName;
            if (!contractsWithInterfaces.Contains(dep.ContractsAssemblyName) && reported.Add(key))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MissingContractInterfaces,
                        Location.None,
                        dep.ModuleName,
                        dep.ContractsAssemblyName
                    )
                );
            }
        }

        // SM0025/SM0026/SM0028/SM0029: Contract implementation diagnostics
        // Group all implementations by interface FQN
        var implsByInterface = new Dictionary<string, List<ContractImplementationRecord>>();
        foreach (var impl in data.ContractImplementations)
        {
            if (!implsByInterface.TryGetValue(impl.InterfaceFqn, out var list))
            {
                list = new List<ContractImplementationRecord>();
                implsByInterface[impl.InterfaceFqn] = list;
            }
            list.Add(impl);
        }

        // SM0028: Non-public implementations
        // SM0029: Abstract implementations
        foreach (var impl in data.ContractImplementations)
        {
            if (!impl.IsPublic)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ContractImplementationNotPublic,
                        Location.None,
                        Strip(impl.ImplementationFqn),
                        Strip(impl.InterfaceFqn)
                    )
                );
            }

            if (impl.IsAbstract)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ContractImplementationIsAbstract,
                        Location.None,
                        Strip(impl.ImplementationFqn),
                        Strip(impl.InterfaceFqn)
                    )
                );
            }
        }

        // SM0025: No implementation for a contract interface
        foreach (var iface in data.ContractInterfaces)
        {
            if (!implsByInterface.ContainsKey(iface.InterfaceName))
            {
                // Derive module name from contracts assembly name
                var moduleName = iface.ContractsAssemblyName;
                if (moduleName.EndsWith(".Contracts", System.StringComparison.Ordinal))
                    moduleName = moduleName.Substring(0, moduleName.Length - ".Contracts".Length);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        NoContractImplementation,
                        Location.None,
                        Strip(iface.InterfaceName),
                        moduleName
                    )
                );
            }
        }

        // SM0026: Multiple valid implementations for the same interface
        foreach (var kvp in implsByInterface)
        {
            var validImpls = new List<ContractImplementationRecord>();
            foreach (var impl in kvp.Value)
            {
                if (impl.IsPublic && !impl.IsAbstract)
                    validImpls.Add(impl);
            }

            if (validImpls.Count > 1)
            {
                var names = new List<string>();
                foreach (var impl in validImpls)
                    names.Add(Strip(impl.ImplementationFqn));

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MultipleContractImplementations,
                        Location.None,
                        Strip(kvp.Key),
                        validImpls[0].ModuleName,
                        string.Join(", ", names)
                    )
                );
            }
        }

        // SM0027/SM0031/SM0032/SM0033/SM0034: Permission diagnostics
        // Track permission values for duplicate detection (value -> class FQN)
        var permissionValueOwners = new Dictionary<string, string>();

        foreach (var perm in data.PermissionClasses)
        {
            var permCleanName = Strip(perm.FullyQualifiedName);

            // SM0032: Not sealed
            if (!perm.IsSealed)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(PermissionClassNotSealed, Location.None, permCleanName)
                );
            }

            foreach (var field in perm.Fields)
            {
                // SM0027: Field is not const string
                if (!field.IsConstString)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            PermissionFieldNotConstString,
                            Location.None,
                            permCleanName,
                            field.FieldName
                        )
                    );
                    continue;
                }

                // SM0031: Value doesn't match {Module}.{Action} pattern (exactly one dot)
                var dotCount = 0;
                foreach (var ch in field.Value)
                {
                    if (ch == '.')
                        dotCount++;
                }
                if (dotCount != 1)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            PermissionValueBadPattern,
                            Location.None,
                            field.Value,
                            permCleanName
                        )
                    );
                }

                // SM0034: Value prefix doesn't match module name
                if (dotCount >= 1)
                {
                    var prefix = field.Value.Substring(0, field.Value.IndexOf('.'));
                    if (!string.Equals(prefix, perm.ModuleName, System.StringComparison.Ordinal))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                PermissionValueWrongPrefix,
                                Location.None,
                                field.Value,
                                perm.ModuleName
                            )
                        );
                    }
                }

                // SM0033: Duplicate permission value
                if (permissionValueOwners.TryGetValue(field.Value, out var existingOwner))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DuplicatePermissionValue,
                            Location.None,
                            field.Value,
                            existingOwner,
                            permCleanName
                        )
                    );
                }
                else
                {
                    permissionValueOwners[field.Value] = permCleanName;
                }
            }
        }

        // SM0035: DTO type in contracts with no public properties
        // Exclude permission classes — they only have const string fields, not properties
        var permissionClassFqns = new HashSet<string>();
        foreach (var perm in data.PermissionClasses)
            permissionClassFqns.Add(perm.FullyQualifiedName);

        foreach (var dto in data.DtoTypes)
        {
            if (
                dto.FullyQualifiedName.Contains(".Contracts.")
                && dto.Properties.Length == 0
                && !permissionClassFqns.Contains(dto.FullyQualifiedName)
            )
            {
                // Extract assembly/namespace context from FQN
                var fqn = Strip(dto.FullyQualifiedName);
                var contractsIdx = fqn.IndexOf(".Contracts.", System.StringComparison.Ordinal);
                var contractsAsm =
                    contractsIdx >= 0 ? fqn.Substring(0, contractsIdx + ".Contracts".Length) : fqn;

                context.ReportDiagnostic(
                    Diagnostic.Create(DtoTypeNoProperties, Location.None, fqn, contractsAsm)
                );
            }
        }

        // SM0038: Infrastructure type (DbContext subclass) in Contracts assembly
        foreach (var dto in data.DtoTypes)
        {
            if (
                dto.FullyQualifiedName.Contains(".Contracts.")
                && dto.FullyQualifiedName.Contains("DbContext")
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        InfrastructureTypeInContracts,
                        Location.None,
                        Strip(dto.FullyQualifiedName)
                    )
                );
            }
        }

        // SM0015: Duplicate view page name across modules
        var seenPages =
            new Dictionary<string, (string EndpointFqn, string ModuleName)>();
        foreach (var module in data.Modules)
        {
            foreach (var view in module.Views)
            {
                if (seenPages.TryGetValue(view.Page, out var existing))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DuplicateViewPageName,
                            Location.None,
                            view.Page,
                            Strip(existing.EndpointFqn),
                            existing.ModuleName,
                            Strip(view.FullyQualifiedName),
                            module.ModuleName
                        )
                    );
                }
                else
                {
                    seenPages[view.Page] = (view.FullyQualifiedName, module.ModuleName);
                }
            }
        }

        // SM0041: View page prefix must match module name
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
                continue;

            var expectedPrefix = module.ModuleName + "/";
            foreach (var view in module.Views)
            {
                if (!view.Page.StartsWith(expectedPrefix, System.StringComparison.Ordinal))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            ViewPagePrefixMismatch,
                            Location.None,
                            Strip(view.FullyQualifiedName),
                            module.ModuleName,
                            view.Page
                        )
                    );
                }
            }
        }

        // SM0042: Module with views but no ViewPrefix
        foreach (var module in data.Modules)
        {
            if (module.Views.Length > 0 && string.IsNullOrEmpty(module.ViewPrefix))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ViewEndpointWithoutViewPrefix,
                        Location.None,
                        module.ModuleName,
                        module.Views.Length,
                        module.ModuleName.ToLowerInvariant()
                    )
                );
            }
        }

        // SM0039: Interceptor depends on contract whose implementation takes a DbContext
        foreach (var interceptor in data.Interceptors)
        {
            foreach (var paramFqn in interceptor.ConstructorParamTypeFqns)
            {
                foreach (var impl in data.ContractImplementations)
                {
                    if (impl.InterfaceFqn == paramFqn && impl.DependsOnDbContext)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                InterceptorDependsOnDbContext,
                                Location.None,
                                Strip(interceptor.FullyQualifiedName),
                                interceptor.ModuleName,
                                Strip(paramFqn)
                            )
                        );
                    }
                }
            }
        }
    }

    private static string Strip(string fqn) => TypeMappingHelpers.StripGlobalPrefix(fqn);

    private static string ExtractShortName(string interfaceName)
    {
        var name = Strip(interfaceName);
        if (name.Contains("."))
            name = name.Substring(name.LastIndexOf('.') + 1);
        if (name.StartsWith("I", System.StringComparison.Ordinal) && name.Length > 1)
            name = name.Substring(1);
        if (name.EndsWith("Contracts", System.StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Contracts".Length);
        return name;
    }
}
