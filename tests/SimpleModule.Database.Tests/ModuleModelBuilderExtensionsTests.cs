using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.Database.Tests;

public sealed class ModuleModelBuilderExtensionsTests : IDisposable
{
    private readonly SharedSqliteDbContext _db;

    public ModuleModelBuilderExtensionsTests()
    {
        var options = new DbContextOptionsBuilder<SharedSqliteDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=shared.db" }
        );
        _db = new SharedSqliteDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void SharedSqliteDb_PrefixesTablesWithModuleName()
    {
        var tableNames = _db.Model.GetEntityTypes().Select(e => e.GetTableName()).ToList();

        tableNames.Should().AllSatisfy(t => t.Should().StartWith("TestModule_"));
    }

    [Fact]
    public void PerModuleDb_DoesNotPrefixTables()
    {
        var options = new DbContextOptionsBuilder<PerModuleDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                DefaultConnection = "Data Source=shared.db",
                ModuleConnections = new Dictionary<string, string>
                {
                    ["TestModule"] = "Data Source=testmodule.db",
                },
            }
        );
        using var db = new PerModuleDbContext(options, dbOptions);

        var tableNames = db.Model.GetEntityTypes().Select(e => e.GetTableName()).ToList();

        tableNames.Should().Contain("Items");
        tableNames.Should().NotContain(t => t!.StartsWith("TestModule_"));
    }

    [Fact]
    public void SharedPostgresDb_SetsSchemaToLowercaseModuleName()
    {
        var options = new DbContextOptionsBuilder<SharedPostgresDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                DefaultConnection =
                    "Host=localhost;Database=mydb;Username=postgres;Password=secret",
            }
        );
        using var db = new SharedPostgresDbContext(options, dbOptions);

        var schemas = db.Model.GetEntityTypes().Select(e => e.GetSchema()).Distinct().ToList();

        schemas.Should().AllBe("testmodule");
    }
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SharedSqliteDbContext(
    DbContextOptions<SharedSqliteDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<TestEntity> Items => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.ApplyModuleSchema("TestModule", dbOptions.Value);
    }
}

public class PerModuleDbContext(
    DbContextOptions<PerModuleDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<TestEntity> Items => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.ApplyModuleSchema("TestModule", dbOptions.Value);
    }
}

public class SharedPostgresDbContext(
    DbContextOptions<SharedPostgresDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<TestEntity> Items => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.ApplyModuleSchema("TestModule", dbOptions.Value);
    }
}
