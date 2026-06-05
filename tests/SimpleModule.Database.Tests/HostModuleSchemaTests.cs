using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.Database.Tests;

/// <summary>
/// Regression tests for #229: a navigation-reachable child entity (a related type that has its
/// own key but no <c>DbSet</c>) must be attributed to its <em>owning</em> module's schema, not
/// swept into the identity module. Exercises
/// <see cref="ModuleModelBuilderExtensions.ApplyHostModuleSchemas"/>, the logic the generated
/// <c>HostDbContext</c> calls.
/// </summary>
public sealed class HostModuleSchemaTests
{
    private static HostLikeDbContext CreateContext(string? provider, string? identityModule)
    {
        var options = new DbContextOptionsBuilder<HostLikeDbContext>()
            .UseSqlite("Data Source=:memory:")
            // Each scenario varies provider/identity but reuses one context type; disable
            // service-provider caching so EF builds a fresh model per test instead of serving a
            // cached one (which would leak one test's schema choices into the next).
            .EnableServiceProviderCaching(false)
            .Options;
        var dbOptions = new DatabaseOptions
        {
            // Looks like SQLite; an explicit Provider (when set) drives the schema strategy.
            DefaultConnection = "Data Source=app.db",
            Provider = provider,
        };
        return new HostLikeDbContext(options, dbOptions, identityModule);
    }

    [Fact]
    public void Sqlite_NavigationOnlyChild_GetsOwningModulePrefix_NotIdentity()
    {
        using var db = CreateContext(provider: null, identityModule: "Users");
        var model = db.Model;

        model.FindEntityType(typeof(Invoice))!.GetTableName().Should().Be("Invoices_Invoices");

        // The child is reachable only through Invoice.Lines (no DbSet of its own). Before the
        // fix it landed in the identity module's prefix (Users_InvoiceLine). It must follow its
        // principal's module instead.
        model
            .FindEntityType(typeof(InvoiceLine))!
            .GetTableName()
            .Should()
            .Be("Invoices_InvoiceLine");

        model.FindEntityType(typeof(Customer))!.GetTableName().Should().Be("Crm_Customers");

        // An entity with no module DbSet and no relationship to one falls back to the identity
        // module — this is what the fallback is actually for (e.g. ASP.NET Identity tables).
        model.FindEntityType(typeof(LooseTable))!.GetTableName().Should().Be("Users_LooseTable");
    }

    [Fact]
    public void Sqlite_GrandchildEntity_InheritsModuleTransitively()
    {
        using var db = CreateContext(provider: null, identityModule: "Users");
        var model = db.Model;

        // InvoiceLineNote is a child of InvoiceLine, which is itself only navigation-reachable.
        // The fixed-point propagation must reach it (Invoice -> InvoiceLine -> InvoiceLineNote).
        model
            .FindEntityType(typeof(InvoiceLineNote))!
            .GetTableName()
            .Should()
            .Be("Invoices_InvoiceLineNote");
    }

    [Fact]
    public void Sqlite_NoIdentityModule_LeavesUnownedEntitiesUnprefixed()
    {
        using var db = CreateContext(provider: null, identityModule: null);
        var model = db.Model;

        model.FindEntityType(typeof(Invoice))!.GetTableName().Should().Be("Invoices_Invoices");
        model
            .FindEntityType(typeof(InvoiceLine))!
            .GetTableName()
            .Should()
            .Be("Invoices_InvoiceLine");

        // No identity module → nothing to sweep the unowned table into; it keeps its bare name.
        model.FindEntityType(typeof(LooseTable))!.GetTableName().Should().Be("LooseTable");
    }

    [Fact]
    public void Postgres_NavigationOnlyChild_GetsOwningModuleSchema_NotIdentity()
    {
        using var db = CreateContext(provider: "PostgreSql", identityModule: "Users");
        var model = db.Model;

        model.FindEntityType(typeof(Invoice))!.GetSchema().Should().Be("invoices");
        model.FindEntityType(typeof(InvoiceLine))!.GetSchema().Should().Be("invoices");
        model.FindEntityType(typeof(Customer))!.GetSchema().Should().Be("crm");
        model.FindEntityType(typeof(LooseTable))!.GetSchema().Should().Be("users");
    }
}

#pragma warning disable CA1812 // Instantiated in tests
internal sealed class Invoice
{
    public int Id { get; set; }
    public List<InvoiceLine> Lines { get; } = [];
}

internal sealed class InvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public List<InvoiceLineNote> Notes { get; } = [];
}

internal sealed class InvoiceLineNote
{
    public int Id { get; set; }
    public int InvoiceLineId { get; set; }
    public string Text { get; set; } = string.Empty;
}

internal sealed class Customer
{
    public int Id { get; set; }
}

internal sealed class LooseTable
{
    public int Id { get; set; }
}

/// <summary>
/// Stands in for the generated <c>HostDbContext</c>: it owns DbSets from several modules and
/// has navigation-reachable child entities (no DbSet) plus an entity with no module at all.
/// </summary>
internal sealed class HostLikeDbContext(
    DbContextOptions<HostLikeDbContext> options,
    DatabaseOptions dbOptions,
    string? identityModule
) : DbContext(options)
{
    private readonly DatabaseOptions _dbOptions = dbOptions;
    private readonly string? _identityModule = identityModule;

    public DbSet<Invoice> Invoices => Set<Invoice>(); // module "Invoices"
    public DbSet<Customer> Customers => Set<Customer>(); // module "Crm"

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // InvoiceLine / InvoiceLineNote are intentionally NOT configured here: they are
        // discovered by EF from the Invoice.Lines / InvoiceLine.Notes navigations, exactly as a
        // module's child collection would be in the unified host model (#229).

        // An entity with no DbSet and no relationship to a module entity — represents an
        // ASP.NET Identity table, which ships no module DbSet.
        modelBuilder.Entity<LooseTable>();

        var moduleByEntityType = new Dictionary<Type, string>
        {
            [typeof(Invoice)] = "Invoices",
            [typeof(Customer)] = "Crm",
        };
        modelBuilder.ApplyHostModuleSchemas(_dbOptions, moduleByEntityType, _identityModule);
    }
}
#pragma warning restore CA1812
