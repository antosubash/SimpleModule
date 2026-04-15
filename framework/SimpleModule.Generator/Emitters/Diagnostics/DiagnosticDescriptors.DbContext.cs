using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static partial class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor DuplicateDbSetPropertyName = new(
        id: "SM0001",
        title: "Duplicate DbSet property name across modules",
        messageFormat: "DbSet property name '{0}' is used by multiple modules: {1} (entity {2}) and {3} (entity {4}). Each module must use unique DbSet property names to avoid table name conflicts in the unified HostDbContext.",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
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

    internal static readonly DiagnosticDescriptor EntityNotInContractsAssembly = new(
        id: "SM0055",
        title: "Entity class must live in a Contracts assembly",
        messageFormat: "Entity '{0}' is exposed as DbSet '{1}' on '{2}' but is declared in assembly '{3}'. Entity classes must be declared in a '.Contracts' assembly so other modules can reference them type-safely through contracts. Move '{0}' to assembly '{4}' (or another '.Contracts' assembly that the module references).",
        category: "SimpleModule.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
