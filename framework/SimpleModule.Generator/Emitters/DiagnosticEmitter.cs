using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

/// <summary>
/// Naming conventions for SimpleModule assemblies. Centralised so the same
/// string literals don't drift between discovery code and diagnostic emission.
/// </summary>
internal static class AssemblyConventions
{
    internal const string FrameworkPrefix = "SimpleModule.";
    internal const string ContractsSuffix = ".Contracts";
    internal const string ModuleSuffix = ".Module";

    /// <summary>
    /// Derives the `.Contracts` sibling assembly name for a SimpleModule
    /// implementation assembly. Strips a trailing <c>.Module</c> suffix first
    /// so <c>SimpleModule.Agents.Module</c> maps to
    /// <c>SimpleModule.Agents.Contracts</c> instead of
    /// <c>SimpleModule.Agents.Module.Contracts</c>.
    /// </summary>
    internal static string GetExpectedContractsAssemblyName(string implementationAssemblyName)
    {
        var baseName = implementationAssemblyName.EndsWith(ModuleSuffix, StringComparison.Ordinal)
            ? implementationAssemblyName.Substring(
                0,
                implementationAssemblyName.Length - ModuleSuffix.Length
            )
            : implementationAssemblyName;
        return baseName + ContractsSuffix;
    }
}

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
        messageFormat: "Circular module dependency detected. Cycle: {0}. {1}To break this cycle, identify which direction is the primary dependency and reverse the other using IMessageBus. For example, if {2} is the primary consumer of {3}: (1) Keep {2} \u2192 {3}.Contracts. (2) Remove {3} \u2192 {2}.Contracts. (3) In {3}, publish events via IMessageBus instead of calling {2} directly. (4) In {2}, add a Wolverine message handler to react to those events. Learn more: https://docs.simplemodule.dev/module-dependencies.",
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

    internal static readonly DiagnosticDescriptor EmptyModuleWarning = new(
        id: "SM0043",
        title: "Module does not override any IModule methods",
        messageFormat: "Module '{0}' implements IModule but does not override any configuration methods (ConfigureServices, ConfigureMenu, etc.). This module will be discovered but has no effect. If this is intentional, add at least ConfigureServices with a comment explaining why.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor MultipleModuleOptions = new(
        id: "SM0044",
        title: "Multiple IModuleOptions for same module",
        messageFormat: "Module '{0}' has multiple IModuleOptions implementations: '{1}' and '{2}'. Each module should have at most one options class. Only the first will be used.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor FeatureClassNotSealed = new(
        id: "SM0045",
        title: "Feature class is not sealed",
        messageFormat: "'{0}' implements IModuleFeatures but is not sealed",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor FeatureFieldNamingViolation = new(
        id: "SM0046",
        title: "Feature field naming violation",
        messageFormat: "Feature '{0}' in '{1}' does not follow the 'ModuleName.FeatureName' pattern",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor DuplicateFeatureName = new(
        id: "SM0047",
        title: "Duplicate feature name",
        messageFormat: "Feature name '{0}' is defined in both '{1}' and '{2}'",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor FeatureFieldNotConstString = new(
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

    private static readonly DiagnosticDescriptor MissingEndpointRouteConst = new(
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
                        LocationHelper.ToLocation(module.Location),
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
                        LocationHelper.ToLocation(module.Location),
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

        // SM0043: Empty module (no IModule methods overridden)
        var moduleNamesWithDbContext = new HashSet<string>(
            data.DbContexts.Select(db => db.ModuleName),
            StringComparer.Ordinal
        );
        foreach (var module in data.Modules)
        {
            if (
                !module.HasConfigureServices
                && !module.HasConfigureEndpoints
                && !module.HasConfigureMenu
                && !module.HasConfigurePermissions
                && !module.HasConfigureMiddleware
                && !module.HasConfigureSettings
                && !module.HasConfigureFeatureFlags
                && module.Endpoints.Length == 0
                && module.Views.Length == 0
                && !moduleNamesWithDbContext.Contains(module.ModuleName)
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        EmptyModuleWarning,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName
                    )
                );
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
                        LocationHelper.ToLocation(ctx.Location),
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
                        LocationHelper.ToLocation(ctx.Location),
                        Strip(ctx.FullyQualifiedName),
                        ctx.ModuleName,
                        0
                    )
                );
            }
        }

        // SM0055: Entity classes must live in a .Contracts assembly.
        // Walks every DbSet in the same pass that also collects EntityFqns
        // for SM0006 below, so we only iterate data.DbContexts once.
        var allEntityFqns = new HashSet<string>();
        foreach (var ctx in data.DbContexts)
        {
            foreach (var dbSet in ctx.DbSets)
            {
                allEntityFqns.Add(dbSet.EntityFqn);

                // Skip entities we can't flag: IdentityDbContext external types,
                // metadata-only symbols (no source location), and anything that
                // lives outside the SimpleModule.* assembly family.
                if (ctx.IsIdentityDbContext)
                    continue;
                if (dbSet.EntityLocation is null)
                    continue;
                if (
                    !dbSet.EntityAssemblyName.StartsWith(
                        AssemblyConventions.FrameworkPrefix,
                        StringComparison.Ordinal
                    )
                )
                    continue;
                if (
                    dbSet.EntityAssemblyName.EndsWith(
                        AssemblyConventions.ContractsSuffix,
                        StringComparison.Ordinal
                    )
                )
                    continue;

                var expectedContractsAssembly =
                    AssemblyConventions.GetExpectedContractsAssemblyName(dbSet.EntityAssemblyName);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        EntityNotInContractsAssembly,
                        LocationHelper.ToLocation(dbSet.EntityLocation),
                        Strip(dbSet.EntityFqn),
                        dbSet.PropertyName,
                        Strip(ctx.FullyQualifiedName),
                        dbSet.EntityAssemblyName,
                        expectedContractsAssembly
                    )
                );
            }
        }

        // SM0006: Entity config for entity not in any DbSet
        // (allEntityFqns was populated above during the SM0055 pass)
        foreach (var config in data.EntityConfigs)
        {
            if (!allEntityFqns.Contains(config.EntityFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        EntityConfigForMissingEntity,
                        LocationHelper.ToLocation(config.Location),
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
                        LocationHelper.ToLocation(config.Location),
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

            // Find location of the first module in the cycle
            SourceLocationRecord? cycleLoc = null;
            foreach (var module in data.Modules)
            {
                if (module.ModuleName == first)
                {
                    cycleLoc = module.Location;
                    break;
                }
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    CircularModuleDependency,
                    LocationHelper.ToLocation(cycleLoc),
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
                    LocationHelper.ToLocation(illegal.Location),
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
                        LocationHelper.ToLocation(iface.Location),
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
                        LocationHelper.ToLocation(iface.Location),
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
                // Find the module's location for this diagnostic
                SourceLocationRecord? depModuleLoc = null;
                foreach (var module in data.Modules)
                {
                    if (module.ModuleName == dep.ModuleName)
                    {
                        depModuleLoc = module.Location;
                        break;
                    }
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MissingContractInterfaces,
                        LocationHelper.ToLocation(depModuleLoc),
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
                        LocationHelper.ToLocation(impl.Location),
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
                        LocationHelper.ToLocation(impl.Location),
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
                        LocationHelper.ToLocation(iface.Location),
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
                        LocationHelper.ToLocation(validImpls[1].Location),
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
                    Diagnostic.Create(
                        PermissionClassNotSealed,
                        LocationHelper.ToLocation(perm.Location),
                        permCleanName
                    )
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
                            LocationHelper.ToLocation(field.Location),
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
                            LocationHelper.ToLocation(field.Location),
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
                                LocationHelper.ToLocation(field.Location),
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
                            LocationHelper.ToLocation(field.Location),
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
        // Exclude permission and feature classes — they only have const string fields, not properties
        var permissionClassFqns = new HashSet<string>();
        foreach (var perm in data.PermissionClasses)
            permissionClassFqns.Add(perm.FullyQualifiedName);
        foreach (var feat in data.FeatureClasses)
            permissionClassFqns.Add(feat.FullyQualifiedName);

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
        var seenPages = new Dictionary<string, (string EndpointFqn, string ModuleName)>();
        foreach (var module in data.Modules)
        {
            foreach (var view in module.Views)
            {
                if (seenPages.TryGetValue(view.Page, out var existing))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DuplicateViewPageName,
                            LocationHelper.ToLocation(view.Location),
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
                            LocationHelper.ToLocation(view.Location),
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
#pragma warning disable CA1308 // Route prefixes are conventionally lowercase
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ViewEndpointWithoutViewPrefix,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName,
                        module.Views.Length,
                        module.ModuleName.ToLowerInvariant()
                    )
                );
#pragma warning restore CA1308
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
                                LocationHelper.ToLocation(interceptor.Location),
                                Strip(interceptor.FullyQualifiedName),
                                interceptor.ModuleName,
                                Strip(paramFqn)
                            )
                        );
                    }
                }
            }
        }

        // SM0044: Multiple IModuleOptions for same module
        var optionsByModule = ModuleOptionsRecord.GroupByModule(data.ModuleOptions);

        foreach (var kvp in optionsByModule)
        {
            if (kvp.Value.Count > 1)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MultipleModuleOptions,
                        LocationHelper.ToLocation(kvp.Value[1].Location),
                        kvp.Key,
                        Strip(kvp.Value[0].FullyQualifiedName),
                        Strip(kvp.Value[1].FullyQualifiedName)
                    )
                );
            }
        }

        // SM0045/SM0046/SM0047/SM0048: Feature flag diagnostics
        var featureValueOwners = new Dictionary<string, string>();

        foreach (var feat in data.FeatureClasses)
        {
            var featCleanName = Strip(feat.FullyQualifiedName);

            // SM0045: Not sealed
            if (!feat.IsSealed)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        FeatureClassNotSealed,
                        LocationHelper.ToLocation(feat.Location),
                        featCleanName
                    )
                );
            }

            foreach (var field in feat.Fields)
            {
                // SM0048: Not a const string
                if (!field.IsConstString)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            FeatureFieldNotConstString,
                            LocationHelper.ToLocation(field.Location),
                            field.FieldName,
                            featCleanName
                        )
                    );
                    continue;
                }

                // SM0046: Naming violation
                if (!field.Value.Contains("."))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            FeatureFieldNamingViolation,
                            LocationHelper.ToLocation(field.Location),
                            field.Value,
                            featCleanName
                        )
                    );
                }

                // SM0047: Duplicate feature name
                if (featureValueOwners.TryGetValue(field.Value, out var existingOwner))
                {
                    if (existingOwner != feat.FullyQualifiedName)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DuplicateFeatureName,
                                LocationHelper.ToLocation(field.Location),
                                field.Value,
                                Strip(existingOwner),
                                featCleanName
                            )
                        );
                    }
                }
                else
                {
                    featureValueOwners[field.Value] = feat.FullyQualifiedName;
                }
            }
        }

        // SM0049: Multiple endpoints (IViewEndpoint) in a single file
        var viewsByFile = new Dictionary<string, List<(string Name, SourceLocationRecord Loc)>>();

        foreach (var module in data.Modules)
        {
            foreach (var view in module.Views)
            {
                if (view.Location is { } loc && !string.IsNullOrEmpty(loc.FilePath))
                {
                    if (!viewsByFile.TryGetValue(loc.FilePath, out var list))
                    {
                        list = new List<(string Name, SourceLocationRecord Loc)>();
                        viewsByFile[loc.FilePath] = list;
                    }

                    list.Add((Strip(view.FullyQualifiedName), loc));
                }
            }
        }

        foreach (var kvp in viewsByFile)
        {
            if (kvp.Value.Count > 1)
            {
                var names = string.Join(", ", kvp.Value.Select(e => e.Name));
                var fileName = Path.GetFileName(kvp.Key);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MultipleEndpointsPerFile,
                        LocationHelper.ToLocation(kvp.Value[1].Loc),
                        fileName,
                        names
                    )
                );
            }
        }

        // SM0052: Module assembly name must follow SimpleModule.{ModuleName} convention
        // SM0053: Module must have matching Contracts assembly
        // These checks only apply when the host project itself is a SimpleModule.* project.
        // User projects (e.g. TestApp.Host) use their own naming conventions.
        var hostIsFramework =
            data.HostAssemblyName?.StartsWith("SimpleModule.", System.StringComparison.Ordinal)
            == true;

        if (hostIsFramework)
        {
            var contractsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in data.ContractsAssemblyNames)
                contractsSet.Add(name);

            foreach (var module in data.Modules)
            {
                if (string.IsNullOrEmpty(module.ModuleName))
                    continue;

                // SM0052: Assembly naming convention
                // Accepted patterns: SimpleModule.{ModuleName} or SimpleModule.{ModuleName}.Module
                // The .Module suffix is allowed when a framework assembly with the same base name exists.
                var expectedAssemblyName = "SimpleModule." + module.ModuleName;
                var expectedModuleSuffix = expectedAssemblyName + ".Module";
                if (
                    !string.IsNullOrEmpty(module.AssemblyName)
                    && !string.Equals(
                        module.AssemblyName,
                        expectedAssemblyName,
                        System.StringComparison.Ordinal
                    )
                    && !string.Equals(
                        module.AssemblyName,
                        expectedModuleSuffix,
                        System.StringComparison.Ordinal
                    )
                    && !string.Equals(
                        module.AssemblyName,
                        data.HostAssemblyName,
                        System.StringComparison.Ordinal
                    )
                )
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            ModuleAssemblyNamingViolation,
                            LocationHelper.ToLocation(module.Location),
                            module.ModuleName,
                            module.AssemblyName
                        )
                    );
                }

                // SM0053: Missing contracts assembly
                var expectedContractsName = "SimpleModule." + module.ModuleName + ".Contracts";
                if (
                    !contractsSet.Contains(expectedContractsName)
                    && !string.Equals(
                        module.AssemblyName,
                        data.HostAssemblyName,
                        System.StringComparison.Ordinal
                    )
                )
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            MissingContractsAssembly,
                            LocationHelper.ToLocation(module.Location),
                            module.ModuleName,
                            module.AssemblyName
                        )
                    );
                }

                // SM0054: Endpoint missing Route const
                foreach (var endpoint in module.Endpoints)
                {
                    if (string.IsNullOrEmpty(endpoint.RouteTemplate))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                MissingEndpointRouteConst,
                                Location.None,
                                Strip(endpoint.FullyQualifiedName)
                            )
                        );
                    }
                }

                foreach (var view in module.Views)
                {
                    if (string.IsNullOrEmpty(view.RouteTemplate))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                MissingEndpointRouteConst,
                                LocationHelper.ToLocation(view.Location),
                                Strip(view.FullyQualifiedName)
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
