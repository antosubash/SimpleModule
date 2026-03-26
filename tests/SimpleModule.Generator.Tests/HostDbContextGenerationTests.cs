using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class HostDbContextGenerationTests
{
    private const string ModuleWithDbContext = """
        using SimpleModule.Core;
        using Microsoft.EntityFrameworkCore;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Options;
        using SimpleModule.Database;

        namespace TestApp.Products.Contracts
        {
            [Dto]
            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
            }
        }

        namespace TestApp.Products
        {
            [Module("Products")]
            public class ProductsModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }

            public class ProductsDbContext : DbContext
            {
                public ProductsDbContext(DbContextOptions<ProductsDbContext> options, IOptions<DatabaseOptions> dbOptions)
                    : base(options) { }

                public DbSet<global::TestApp.Products.Contracts.Product> Products => Set<global::TestApp.Products.Contracts.Product>();
            }
        }
        """;

    private const string ModuleWithIdentityDbContext = """
        using SimpleModule.Core;
        using Microsoft.EntityFrameworkCore;
        using Microsoft.AspNetCore.Identity;
        using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Options;
        using SimpleModule.Database;

        namespace TestApp.Users.Entities
        {
            public class AppUser : IdentityUser { }
            public class AppRole : IdentityRole { }
        }

        namespace TestApp.Users
        {
            [Module("Users")]
            public class UsersModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }

            public class UsersDbContext : IdentityDbContext<global::TestApp.Users.Entities.AppUser, global::TestApp.Users.Entities.AppRole, string>
            {
                public UsersDbContext(DbContextOptions<UsersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                    : base(options) { }
            }
        }
        """;

    private const string ModuleWithEntityConfig = """
        using SimpleModule.Core;
        using Microsoft.EntityFrameworkCore;
        using Microsoft.EntityFrameworkCore.Metadata.Builders;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Options;
        using SimpleModule.Database;

        namespace TestApp.Orders.Contracts
        {
            [Dto]
            public class Order
            {
                public int Id { get; set; }
            }

            [Dto]
            public class OrderItem
            {
                public int ProductId { get; set; }
                public int Quantity { get; set; }
            }
        }

        namespace TestApp.Orders
        {
            [Module("Orders")]
            public class OrdersModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }

            public class OrdersDbContext : DbContext
            {
                public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                    : base(options) { }

                public DbSet<global::TestApp.Orders.Contracts.Order> Orders => Set<global::TestApp.Orders.Contracts.Order>();
                public DbSet<global::TestApp.Orders.Contracts.OrderItem> OrderItems => Set<global::TestApp.Orders.Contracts.OrderItem>();
            }
        }

        namespace TestApp.Orders.EntityConfigurations
        {
            public class OrderItemConfiguration : IEntityTypeConfiguration<global::TestApp.Orders.Contracts.OrderItem>
            {
                public void Configure(EntityTypeBuilder<global::TestApp.Orders.Contracts.OrderItem> builder)
                {
                    builder.HasKey("OrderId", nameof(global::TestApp.Orders.Contracts.OrderItem.ProductId));
                }
            }
        }
        """;

    #region Basic emission

    [Fact]
    public void ModuleWithDbContext_EmitsHostDbContext()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void ModuleWithoutDbContext_DoesNotEmitHostDbContext()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("NoDb")]
            public class NoDbModule : IModule { }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .NotContain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void HostDbContext_HasAutoGeneratedHeader()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().StartWith("// <auto-generated/>");
    }

    [Fact]
    public void HostDbContext_HasCorrectNamespace()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("namespace TestAssembly;");
    }

    #endregion

    #region DbContext base class

    [Fact]
    public void PlainDbContext_HostExtendsDbContext()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain(") : DbContext(options)");
        source.Should().NotContain("IdentityDbContext");
    }

    [Fact]
    public void IdentityDbContext_HostExtendsIdentityDbContext()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithIdentityDbContext
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("IdentityDbContext<");
        source.Should().Contain("global::TestApp.Users.Entities.AppUser");
        source.Should().Contain("global::TestApp.Users.Entities.AppRole");
        source.Should().Contain("string>(options)");
    }

    [Fact]
    public void IdentityDbContext_CallsBaseOnModelCreating()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithIdentityDbContext
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("base.OnModelCreating(builder)");
    }

    [Fact]
    public void PlainDbContext_DoesNotCallBaseOnModelCreating()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().NotContain("base.OnModelCreating(builder)");
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_HasDbContextOptionsParameter()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("DbContextOptions<HostDbContext> options");
    }

    [Fact]
    public void Constructor_HasDatabaseOptionsParameter()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("IOptions<DatabaseOptions> dbOptions");
    }

    #endregion

    #region DbSet properties

    [Fact]
    public void SingleModule_DbSetPropertiesPresent()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("DbSet<global::TestApp.Products.Contracts.Product> Products");
        source.Should().Contain("Set<global::TestApp.Products.Contracts.Product>()");
    }

    [Fact]
    public void MultipleModules_AllDbSetsPresent()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithDbContext,
            ModuleWithEntityConfig
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("Products");
        source.Should().Contain("Orders");
        source.Should().Contain("OrderItems");
    }

    [Fact]
    public void DuplicateEntityAcrossModules_DeduplicatedToSingleDbSet()
    {
        // Both modules have a DbSet for the same entity type
        var moduleA = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Shared
            {
                public class SharedEntity
                {
                    public int Id { get; set; }
                }
            }

            namespace TestApp.ModuleA
            {
                [Module("ModuleA")]
                public class ModuleAModule : IModule { }

                public class ModuleADbContext : DbContext
                {
                    public ModuleADbContext(DbContextOptions<ModuleADbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Shared.SharedEntity> SharedEntities => Set<global::TestApp.Shared.SharedEntity>();
                }
            }
            """;

        var moduleB = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.ModuleB
            {
                [Module("ModuleB")]
                public class ModuleBModule : IModule { }

                public class ModuleBDbContext : DbContext
                {
                    public ModuleBDbContext(DbContextOptions<ModuleBDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Shared.SharedEntity> SharedEntities => Set<global::TestApp.Shared.SharedEntity>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(moduleA, moduleB);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        // Should appear only once
        var count = CountOccurrences(source, "DbSet<global::TestApp.Shared.SharedEntity>");
        count.Should().Be(1);
    }

    #endregion

    #region IEntityTypeConfiguration

    [Fact]
    public void EntityTypeConfiguration_ApplyConfigurationEmitted()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithEntityConfig);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source
            .Should()
            .Contain(
                "ApplyConfiguration(new global::TestApp.Orders.EntityConfigurations.OrderItemConfiguration())"
            );
    }

    [Fact]
    public void NoEntityTypeConfiguration_NoApplyConfigurationSection()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().NotContain("ApplyConfiguration");
    }

    #endregion

    #region Schema isolation

    [Fact]
    public void SchemaIsolation_NonSqlite_SetsSchemaPerModule()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithDbContext,
            ModuleWithEntityConfig
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("if (provider != DatabaseProvider.Sqlite)");
        source.Should().Contain(".Metadata.SetSchema(\"products\")");
        source.Should().Contain(".Metadata.SetSchema(\"orders\")");
    }

    [Fact]
    public void SchemaIsolation_Sqlite_PrefixesTables()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithDbContext,
            ModuleWithEntityConfig
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain(".ToTable(\"Products_\"");
        source.Should().Contain(".ToTable(\"Orders_\"");
    }

    [Fact]
    public void IdentityFallbackSchema_AssignsRemainingEntitiesToIdentityModule()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithDbContext,
            ModuleWithIdentityDbContext
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        // Identity tables (not explicitly assigned) should get the Users schema
        source.Should().Contain("if (entityType.GetSchema() is null)");
        source.Should().Contain("entityType.SetSchema(\"users\")");
    }

    [Fact]
    public void NoIdentityDbContext_NoFallbackSchemaLoop()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().NotContain("if (entityType.GetSchema() is null)");
    }

    [Fact]
    public void SchemaIsolation_UsesDatabaseProviderDetector()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(ModuleWithDbContext);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("DatabaseProviderDetector.Detect(");
        source.Should().Contain("dbOptions.Value.DefaultConnection");
        source.Should().Contain("dbOptions.Value.Provider");
    }

    [Fact]
    public void Sqlite_IdentityTablesPrefixed()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithDbContext,
            ModuleWithIdentityDbContext
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        source.Should().Contain("Prefix Identity tables for SQLite");
        source.Should().Contain("entityType.SetTableName(\"Users_\" + tableName)");
    }

    [Fact]
    public void Sqlite_IdentityPrefixing_SkipsAlreadyPrefixedTables()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithDbContext,
            ModuleWithIdentityDbContext
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);
        // Should check for Products_ prefix before applying Users_ prefix
        source.Should().Contain("tableName.StartsWith(\"Products_\"");
    }

    #endregion

    #region Abstract/static DbContext ignored

    [Fact]
    public void AbstractDbContext_IsIgnored()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            public abstract class AbstractDbContext : DbContext
            {
                protected AbstractDbContext(DbContextOptions options, IOptions<DatabaseOptions> dbOptions)
                    : base(options) { }

                public DbSet<TestApp.SomeEntity> Items => Set<TestApp.SomeEntity>();
            }

            public class SomeEntity
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .NotContain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));
    }

    #endregion

    #region Duplicate table name diagnostic

    [Fact]
    public void DuplicateDbSetPropertyName_ReportsDiagnostic()
    {
        var moduleA = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.ModuleA;

            public class EntityA { public int Id { get; set; } }

            [Module("ModuleA")]
            public class ModuleAModule : IModule { }

            public class ModuleADbContext : DbContext
            {
                public ModuleADbContext(DbContextOptions<ModuleADbContext> options, IOptions<DatabaseOptions> dbOptions)
                    : base(options) { }

                public DbSet<EntityA> Items => Set<EntityA>();
            }
            """;

        var moduleB = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.ModuleB;

            public class EntityB { public int Id { get; set; } }

            [Module("ModuleB")]
            public class ModuleBModule : IModule { }

            public class ModuleBDbContext : DbContext
            {
                public ModuleBDbContext(DbContextOptions<ModuleBDbContext> options, IOptions<DatabaseOptions> dbOptions)
                    : base(options) { }

                public DbSet<EntityB> Items => Set<EntityB>();
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(moduleA, moduleB);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0001");

        // Should NOT emit HostDbContext when there are conflicts
        result
            .GeneratedTrees.Should()
            .NotContain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateDbSetPropertyName_DiagnosticContainsModuleNames()
    {
        var moduleA = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Alpha;

            public class AlphaEntity { public int Id { get; set; } }

            [Module("Alpha")]
            public class AlphaModule : IModule { }

            public class AlphaDbContext : DbContext
            {
                public AlphaDbContext(DbContextOptions<AlphaDbContext> options, IOptions<DatabaseOptions> dbOptions)
                    : base(options) { }

                public DbSet<AlphaEntity> Records => Set<AlphaEntity>();
            }
            """;

        var moduleB = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Beta;

            public class BetaEntity { public int Id { get; set; } }

            [Module("Beta")]
            public class BetaModule : IModule { }

            public class BetaDbContext : DbContext
            {
                public BetaDbContext(DbContextOptions<BetaDbContext> options, IOptions<DatabaseOptions> dbOptions)
                    : base(options) { }

                public DbSet<BetaEntity> Records => Set<BetaEntity>();
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(moduleA, moduleB);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        var diagnostic = diagnostics.First(d => d.Id == "SM0001");
        var message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("Records");
        message.Should().Contain("Alpha");
        message.Should().Contain("Beta");
    }

    [Fact]
    public void UniqueDbSetPropertyNames_NoDiagnostic()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithDbContext,
            ModuleWithEntityConfig
        );
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0001");
    }

    #endregion

    #region Multiple modules combined

    [Fact]
    public void IdentityAndPlainModules_CombinedCorrectly()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithDbContext,
            ModuleWithIdentityDbContext,
            ModuleWithEntityConfig
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var source = GetHostDbContext(result);

        // Should extend IdentityDbContext
        source.Should().Contain("IdentityDbContext<");

        // Should have all DbSets
        source.Should().Contain("Products");
        source.Should().Contain("Orders");
        source.Should().Contain("OrderItems");

        // Should apply entity configs
        source
            .Should()
            .Contain(
                "ApplyConfiguration(new global::TestApp.Orders.EntityConfigurations.OrderItemConfiguration()"
            );

        // Should have schemas for all modules
        source.Should().Contain("SetSchema(\"products\")");
        source.Should().Contain("SetSchema(\"orders\")");
        source.Should().Contain("SetSchema(\"users\")");
    }

    #endregion

    #region Edge cases

    [Fact]
    public void DbContextThroughIntermediateBaseClass_IsDiscovered()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Auditing
            {
                public class AuditEntry
                {
                    public int Id { get; set; }
                    public string Action { get; set; } = "";
                }

                public class BaseDbContext : DbContext
                {
                    public BaseDbContext(DbContextOptions options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }

                [Module("Auditing")]
                public class AuditingModule : IModule
                {
                    public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class AuditingDbContext : BaseDbContext
                {
                    public AuditingDbContext(DbContextOptions<AuditingDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options, dbOptions) { }

                    public DbSet<global::TestApp.Auditing.AuditEntry> AuditEntries => Set<global::TestApp.Auditing.AuditEntry>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var hostSource = GetHostDbContext(result);
        hostSource.Should().Contain("DbSet<global::TestApp.Auditing.AuditEntry> AuditEntries");
    }

    [Fact]
    public void PrivateDbSetProperties_AreIgnored()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Secrets
            {
                public class PublicEntity
                {
                    public int Id { get; set; }
                }

                public class InternalEntity
                {
                    public int Id { get; set; }
                }

                [Module("Secrets")]
                public class SecretsModule : IModule
                {
                    public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class SecretsDbContext : DbContext
                {
                    public SecretsDbContext(DbContextOptions<SecretsDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Secrets.PublicEntity> PublicEntities => Set<global::TestApp.Secrets.PublicEntity>();
                    private DbSet<global::TestApp.Secrets.InternalEntity> InternalEntities => Set<global::TestApp.Secrets.InternalEntity>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var hostSource = GetHostDbContext(result);
        hostSource.Should().Contain("DbSet<global::TestApp.Secrets.PublicEntity> PublicEntities");
        hostSource.Should().NotContain("InternalEntities");
    }

    [Fact]
    public void NonDbSetProperties_AreIgnored()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Metadata
            {
                public class MetaItem
                {
                    public int Id { get; set; }
                }

                [Module("Metadata")]
                public class MetadataModule : IModule
                {
                    public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class MetadataDbContext : DbContext
                {
                    public MetadataDbContext(DbContextOptions<MetadataDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public string ConnectionInfo { get; set; } = "";
                    public DbSet<global::TestApp.Metadata.MetaItem> MetaItems => Set<global::TestApp.Metadata.MetaItem>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var hostSource = GetHostDbContext(result);
        hostSource.Should().Contain("DbSet<global::TestApp.Metadata.MetaItem> MetaItems");
        hostSource.Should().NotContain("ConnectionInfo");
    }

    [Fact]
    public void AbstractEntityConfiguration_IsIgnored()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Catalog
            {
                public class CatalogItem
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                [Module("Catalog")]
                public class CatalogModule : IModule
                {
                    public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class CatalogDbContext : DbContext
                {
                    public CatalogDbContext(DbContextOptions<CatalogDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Catalog.CatalogItem> CatalogItems => Set<global::TestApp.Catalog.CatalogItem>();
                }

                public abstract class BaseCatalogConfiguration : IEntityTypeConfiguration<global::TestApp.Catalog.CatalogItem>
                {
                    public abstract void Configure(EntityTypeBuilder<global::TestApp.Catalog.CatalogItem> builder);
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var hostSource = GetHostDbContext(result);
        hostSource.Should().NotContain("ApplyConfiguration");
        hostSource.Should().NotContain("BaseCatalogConfiguration");
    }

    [Fact]
    public void IdentityDbContextOnly_NoExtraDbSets_EmitsHostDbContext()
    {
        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(
            ModuleWithIdentityDbContext
        );
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var hostSource = GetHostDbContext(result);

        // Should extend IdentityDbContext
        hostSource.Should().Contain("IdentityDbContext<");
        hostSource.Should().Contain("global::TestApp.Users.Entities.AppUser");
        hostSource.Should().Contain("global::TestApp.Users.Entities.AppRole");

        // Should have zero extra DbSet properties (no "=>" expression body for DbSet)
        hostSource.Should().NotContain("=> Set<");

        // Should still have schema isolation
        hostSource.Should().Contain("SetSchema(\"users\")");
    }

    [Fact]
    public void EntityNameConflictsAcrossNamespaces_BothAppearWithCorrectFQN()
    {
        var moduleA = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Products
            {
                public class Item
                {
                    public int Id { get; set; }
                    public string ProductName { get; set; } = "";
                }

                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class ProductsDbContext : DbContext
                {
                    public ProductsDbContext(DbContextOptions<ProductsDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Products.Item> ProductItems => Set<global::TestApp.Products.Item>();
                }
            }
            """;

        var moduleB = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Orders
            {
                public class Item
                {
                    public int Id { get; set; }
                    public int Quantity { get; set; }
                }

                [Module("Orders")]
                public class OrdersModule : IModule
                {
                    public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class OrdersDbContext : DbContext
                {
                    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Orders.Item> OrderItems => Set<global::TestApp.Orders.Item>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(moduleA, moduleB);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var hostSource = GetHostDbContext(result);
        hostSource.Should().Contain("DbSet<global::TestApp.Products.Item> ProductItems");
        hostSource.Should().Contain("DbSet<global::TestApp.Orders.Item> OrderItems");
    }

    [Fact]
    public void MultipleEntityConfigs_AllApplyConfigurationCallsEmitted()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Inventory
            {
                public class Warehouse
                {
                    public int Id { get; set; }
                    public string Location { get; set; } = "";
                }

                public class StockItem
                {
                    public int Id { get; set; }
                    public int WarehouseId { get; set; }
                }

                [Module("Inventory")]
                public class InventoryModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class InventoryDbContext : DbContext
                {
                    public InventoryDbContext(DbContextOptions<InventoryDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Inventory.Warehouse> Warehouses => Set<global::TestApp.Inventory.Warehouse>();
                    public DbSet<global::TestApp.Inventory.StockItem> StockItems => Set<global::TestApp.Inventory.StockItem>();
                }
            }

            namespace TestApp.Inventory.EntityConfigurations
            {
                public class WarehouseConfiguration : IEntityTypeConfiguration<global::TestApp.Inventory.Warehouse>
                {
                    public void Configure(EntityTypeBuilder<global::TestApp.Inventory.Warehouse> builder)
                    {
                        builder.HasKey(w => w.Id);
                    }
                }

                public class StockItemConfiguration : IEntityTypeConfiguration<global::TestApp.Inventory.StockItem>
                {
                    public void Configure(EntityTypeBuilder<global::TestApp.Inventory.StockItem> builder)
                    {
                        builder.HasKey(s => s.Id);
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var hostSource = GetHostDbContext(result);
        hostSource
            .Should()
            .Contain(
                "ApplyConfiguration(new global::TestApp.Inventory.EntityConfigurations.WarehouseConfiguration())"
            );
        hostSource
            .Should()
            .Contain(
                "ApplyConfiguration(new global::TestApp.Inventory.EntityConfigurations.StockItemConfiguration())"
            );
    }

    [Fact]
    public void DbSetWithGenericEntityType_PreservesGenericInGeneratedCode()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp
            {
                public class AuditLog<T>
                {
                    public int Id { get; set; }
                    public T Value { get; set; } = default!;
                }
            }

            namespace TestApp.Logging
            {
                [Module("Logging")]
                public class LoggingModule : IModule
                {
                    public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class LoggingDbContext : DbContext
                {
                    public LoggingDbContext(DbContextOptions<LoggingDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.AuditLog<string>> AuditLogs => Set<global::TestApp.AuditLog<string>>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var hostSource = GetHostDbContext(result);
        hostSource.Should().Contain("DbSet<global::TestApp.AuditLog<string>> AuditLogs");
    }

    #endregion

    #region Helpers

    private static string GetHostDbContext(Microsoft.CodeAnalysis.GeneratorDriverRunResult result)
    {
        return result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
    }

    private static int CountOccurrences(string source, string substring)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }

    #endregion
}
