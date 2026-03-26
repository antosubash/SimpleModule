---
outline: deep
---

# Database

SimpleModule provides a multi-provider database layer with automatic schema isolation per module. Each module gets its own `DbContext` and its own namespace in the database, whether that is table prefixes (SQLite) or schemas (PostgreSQL, SQL Server).

## Multi-Provider Support

Three database providers are supported out of the box:

| Provider | Schema Isolation | Detection Heuristic |
|----------|-----------------|---------------------|
| **SQLite** | Table name prefixes (`Products_Products`) | Connection string contains `Data Source=` |
| **PostgreSQL** | Database schemas (`products.Products`) | Connection string contains `Host=` |
| **SQL Server** | Database schemas (`products.Products`) | Connection string contains `Initial Catalog=`, `Server=.\`, or `Server=(` |

The provider is auto-detected from the connection string. You can also set it explicitly in configuration:

```json
{
  "Database": {
    "DefaultConnection": "Data Source=app.db",
    "Provider": "Sqlite"
  }
}
```

Valid provider values: `"Sqlite"`, `"PostgreSql"`, `"SqlServer"`.

### Per-Module Connection Strings

By default, all modules share the `DefaultConnection`. If you need a module to use a separate database, add it to `ModuleConnections`:

```json
{
  "Database": {
    "DefaultConnection": "Host=localhost;Database=myapp;...",
    "ModuleConnections": {
      "AuditLogs": "Host=localhost;Database=myapp_audit;..."
    }
  }
}
```

When a module has its own connection string, schema isolation is skipped for that module since it already has a dedicated database.

## One DbContext Per Module

Each module registers its own `DbContext` using the `AddModuleDbContext<T>` extension method. This method handles provider detection, connection string resolution, and interceptor registration:

```csharp
[Module(
    ProductsConstants.ModuleName,
    RoutePrefix = ProductsConstants.RoutePrefix,
    ViewPrefix = "/products"
)]
public class ProductsModule : IModule
{
    public void ConfigureServices(
        IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProductsDbContext>(
            configuration, ProductsConstants.ModuleName);
    }
}
```

### The ProductsDbContext

Here is a complete example of a module DbContext:

```csharp
public class ProductsDbContext(
    DbContextOptions<ProductsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyModuleSchema("Products", dbOptions.Value);
    }

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<ProductId>()
            .HaveConversion<ProductId.EfCoreValueConverter,
                            ProductId.EfCoreValueComparer>();
    }
}
```

Key points:

1. The constructor uses **primary constructor** syntax with `DbContextOptions<T>` and `IOptions<DatabaseOptions>`
2. `OnModelCreating` applies entity configurations and then calls `ApplyModuleSchema` to set up schema isolation
3. `ConfigureConventions` registers Vogen value converters for strongly-typed IDs

::: warning
Always call `modelBuilder.ApplyModuleSchema(moduleName, dbOptions.Value)` in `OnModelCreating`. Without it, your module's tables will not be isolated and may collide with other modules.
:::

## Schema Isolation

The `ApplyModuleSchema` extension method automatically applies the correct isolation strategy based on the database provider:

**SQLite** -- Prefixes all table names with the module name:

```
Products_Products
Products_Categories
Orders_Orders
Orders_OrderItems
```

**PostgreSQL / SQL Server** -- Creates separate schemas:

```sql
products.Products
products.Categories
orders.Orders
orders.OrderItems
```

The implementation:

```csharp
public static void ApplyModuleSchema(
    this ModelBuilder modelBuilder,
    string moduleName,
    DatabaseOptions dbOptions)
{
    var hasOwnConnection = dbOptions.ModuleConnections.ContainsKey(moduleName);
    if (hasOwnConnection)
        return; // Module has its own database, no prefix needed

    var provider = DatabaseProviderDetector.Detect(connectionString);

    if (provider == DatabaseProvider.Sqlite)
    {
        var prefix = $"{moduleName}_";
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null
                && !tableName.StartsWith(prefix, StringComparison.Ordinal))
            {
                entity.SetTableName($"{prefix}{tableName}");
            }
        }
    }
    else
    {
        var schema = moduleName.ToLowerInvariant();
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetSchema(schema);
        }
    }
}
```

## Entity Configurations

Use `IEntityTypeConfiguration<T>` to define entity mappings. Keep these in an `EntityConfigurations` directory in your module:

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.Name).IsRequired();
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");

        builder.HasData(GenerateSeedProducts());
    }

    private static Product[] GenerateSeedProducts()
    {
        var id = 0;
        var faker = new Faker<Product>()
            .UseSeed(54321)
            .RuleFor(p => p.Id, _ => ProductId.From(++id))
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Price, f => decimal.Parse(
                f.Commerce.Price(10, 1000),
                CultureInfo.InvariantCulture));

        return faker.Generate(10).ToArray();
    }
}
```

Apply configurations in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(new ProductConfiguration());
    modelBuilder.ApplyModuleSchema("Products", dbOptions.Value);
}
```

::: tip
The example above uses [Bogus](https://github.com/bchavez/Bogus) to generate deterministic seed data with `UseSeed()`. This is useful for development and testing.
:::

## ModuleDbContextInfo

When you call `AddModuleDbContext<T>`, a `ModuleDbContextInfo` record is automatically registered in DI:

```csharp
public sealed record ModuleDbContextInfo(string ModuleName, Type DbContextType);
```

The framework uses this at startup to discover all module databases and run initialization (e.g., `EnsureCreated()` or migrations). You generally do not interact with this type directly.

## EF Core Interceptor DI Pattern

SaveChanges interceptors can cause circular dependencies when they depend on services that themselves depend on a `DbContext`. For example:

```
SaveChangesInterceptor -> ISettingsContracts -> SettingsService -> SettingsDbContext
```

This creates a deadlock during DI construction. The solution is to resolve runtime dependencies at interception time, not in the constructor.

### Correct Pattern

```csharp
public sealed class AuditSaveChangesInterceptor(
    IAuditContext auditContext,
    AuditChannel channel,
    IServiceProvider? serviceProvider = null  // Nullable for optionality
) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(
                eventData, result, cancellationToken);

        // Resolve settings at interception time, not constructor time
        ISettingsContracts? settings = null;
        if (serviceProvider is not null)
        {
            settings = serviceProvider.GetService<ISettingsContracts>();
        }

        if (settings is not null)
        {
            var raw = await settings.GetSettingAsync(
                "auditlogs.capture.changes", SettingScope.System);
            if (string.Equals(
                raw, "false", StringComparison.OrdinalIgnoreCase))
                return await base.SavingChangesAsync(
                    eventData, result, cancellationToken);
        }

        // ... interceptor logic
        return await base.SavingChangesAsync(
            eventData, result, cancellationToken);
    }
}
```

### Anti-Pattern

```csharp
// WRONG: direct dependency on a service that depends on DbContext
public sealed class BadInterceptor(
    ISettingsContracts settings  // Circular dependency!
) : SaveChangesInterceptor { }
```

::: danger
Never inject services that transitively depend on a `DbContext` into interceptor constructors. Inject `IServiceProvider?` as an optional dependency and resolve the service inside the interception method.
:::

### How Interceptors Are Registered

The `AddModuleDbContext` method automatically resolves all registered `ISaveChangesInterceptor` instances and adds them to the DbContext options:

```csharp
// Inside AddModuleDbContext
options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
```

Register your interceptor in the module's `ConfigureServices`:

```csharp
services.AddScoped<ISaveChangesInterceptor, AuditSaveChangesInterceptor>();
```

All interceptors registered this way apply to **all** module DbContexts. If your interceptor should skip its own module's context, check the context type:

```csharp
var contextType = eventData.Context.GetType();
if (contextType == typeof(AuditLogsDbContext))
    return await base.SavingChangesAsync(eventData, result, cancellationToken);
```

## Development: EnsureCreated

In non-production environments, the framework automatically initializes databases at startup. This means you can iterate quickly without running migrations manually during development.

::: tip
For development, `EnsureCreated()` is called automatically. You do not need to run any migration commands to get started.
:::

## Production: EF Core Migrations

For production deployments, use EF Core migrations scoped to each module:

```bash
# Generate a migration for the Products module
dotnet ef migrations add InitialCreate \
  --project modules/Products/src/Products \
  --startup-project template/SimpleModule.Host \
  --context ProductsDbContext

# Apply migrations
dotnet ef database update \
  --project modules/Products/src/Products \
  --startup-project template/SimpleModule.Host \
  --context ProductsDbContext
```

Each module manages its own migration history independently, since each has its own `DbContext` with its own schema.

## Configuration Reference

Full `Database` configuration section:

```json
{
  "Database": {
    "DefaultConnection": "Data Source=app.db",
    "Provider": "Sqlite",
    "ModuleConnections": {
      "AuditLogs": "Host=localhost;Database=audit;Username=app;Password=..."
    }
  }
}
```

| Property | Type | Description |
|----------|------|-------------|
| `DefaultConnection` | `string` | Connection string shared by all modules (required) |
| `Provider` | `string?` | Explicit provider override. Auto-detected from connection string if omitted |
| `ModuleConnections` | `Dictionary<string, string>` | Per-module connection strings for database isolation |
