# Database & Data Access Patterns

## Module DbContext

Each module that owns data creates its own `DbContext`:

```csharp
public sealed class ProductsDbContext : DbContext, IModuleDbContext
{
    private readonly IOptions<DatabaseOptions> _dbOptions;

    public static string SchemaName => "products";

    public DbSet<Product> Products => Set<Product>();

    public ProductsDbContext(
        DbContextOptions<ProductsDbContext> options,
        IOptions<DatabaseOptions> dbOptions) : base(options)
    {
        _dbOptions = dbOptions;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductsDbContext).Assembly);
        modelBuilder.ApplyModuleSchema(SchemaName, _dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Register Vogen value object converters
        configurationBuilder.Properties<ProductId>()
            .HaveConversion<ProductId.EfCoreValueConverter>();
    }
}
```

## Registration

Register in the module's `ConfigureServices`:

```csharp
services.AddModuleDbContext<ProductsDbContext>(configuration, ProductsConstants.ModuleName);
```

## Schema Isolation

- **PostgreSQL/SQL Server**: Uses database schemas (e.g., `products.Products`)
- **SQLite**: Uses table name prefixes (e.g., `products_Products`)
- Applied automatically via `modelBuilder.ApplyModuleSchema()`

## Entity Configuration

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
    }
}
```

## Seed Data with Bogus

```csharp
builder.HasData(
    new Faker<Product>()
        .UseSeed(54321)    // Deterministic for reproducible seeds
        .RuleFor(p => p.Id, f => ProductId.From(f.IndexFaker + 1))
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price()))
        .Generate(10));
```

## Value Objects (Vogen)

Define in Contracts assembly:

```csharp
[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct ProductId;
```

This auto-generates:
- JSON serialization/deserialization
- EF Core value converter (`ProductId.EfCoreValueConverter`)
- Route parameter binding
- Equality, `ToString()`, hash code

## Database Provider Detection

Auto-detected from connection string:
- **PostgreSQL**: Contains `Host=`
- **SQL Server**: Contains `Initial Catalog=`, `Server=.\`, or `Server=(`
- **SQLite**: Contains `Data Source=`
- Explicit override: `Database:Provider` in configuration

## Service Pattern

```csharp
public sealed class ProductService : IProductContracts
{
    private readonly ProductsDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductsDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _db.Products.ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(ProductId id)
    {
        return await _db.Products.FindAsync(id);
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product { Name = request.Name, Price = request.Price };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        LogProductCreated(product.Id, product.Name);
        return product;
    }
}
```

## Source-Generated Logging

```csharp
public sealed partial class ProductService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductId} '{Name}' created")]
    partial void LogProductCreated(ProductId productId, string name);
}
```
