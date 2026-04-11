using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class DiagnosticTests
{
    #region SM0002: Empty module name

    [Fact]
    public void SM0002_EmptyModuleName_ReportsWarning()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("")]
                public class BadModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0002");
        var diag = diagnostics.First(d => d.Id == "SM0002");
        diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture)
            .Should()
            .Contain("BadModule");
    }

    [Fact]
    public void SM0002_NonEmptyModuleName_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Valid")]
                public class ValidModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0002");
    }

    #endregion

    #region SM0003: Multiple IdentityDbContexts

    [Fact]
    public void SM0003_MultipleIdentityDbContexts_ReportsError()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.AspNetCore.Identity;
            using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Users
            {
                public class UserA : IdentityUser { }
                public class RoleA : IdentityRole { }

                [Module("Users")]
                public class UsersModule : IModule { }

                public class UsersDbContext : IdentityDbContext<UserA, RoleA, string>
                {
                    public UsersDbContext(DbContextOptions<UsersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }
            }

            namespace TestApp.Accounts
            {
                public class UserB : IdentityUser { }
                public class RoleB : IdentityRole { }

                [Module("Accounts")]
                public class AccountsModule : IModule { }

                public class AccountsDbContext : IdentityDbContext<UserB, RoleB, string>
                {
                    public AccountsDbContext(DbContextOptions<AccountsDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0003");
        var diag = diagnostics.First(d => d.Id == "SM0003");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("Users");
        message.Should().Contain("Accounts");

        // Should NOT emit HostDbContext when there's a conflict
        result
            .GeneratedTrees.Should()
            .NotContain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void SM0003_SingleIdentityDbContext_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.AspNetCore.Identity;
            using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Users
            {
                public class AppUser : IdentityUser { }
                public class AppRole : IdentityRole { }

                [Module("Users")]
                public class UsersModule : IModule { }

                public class UsersDbContext : IdentityDbContext<AppUser, AppRole, string>
                {
                    public UsersDbContext(DbContextOptions<UsersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0003");
    }

    #endregion

    #region SM0006: Entity config for entity not in any DbSet

    [Fact]
    public void SM0006_EntityConfigForMissingEntity_ReportsWarning()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Orders
            {
                public class Order { public int Id { get; set; } }
                public class OrphanEntity { public int Id { get; set; } }

                [Module("Orders")]
                public class OrdersModule : IModule { }

                public class OrdersDbContext : DbContext
                {
                    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<Order> Orders => Set<Order>();
                    // Note: no DbSet for OrphanEntity
                }

                public class OrphanConfiguration : IEntityTypeConfiguration<OrphanEntity>
                {
                    public void Configure(EntityTypeBuilder<OrphanEntity> builder)
                    {
                        builder.HasKey(e => e.Id);
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0006");
        var diag = diagnostics.First(d => d.Id == "SM0006");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("OrphanEntity");
        message.Should().Contain("OrphanConfiguration");
    }

    [Fact]
    public void SM0006_EntityConfigForExistingEntity_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Orders
            {
                public class Order { public int Id { get; set; } }

                [Module("Orders")]
                public class OrdersModule : IModule { }

                public class OrdersDbContext : DbContext
                {
                    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<Order> Orders => Set<Order>();
                }

                public class OrderConfiguration : IEntityTypeConfiguration<Order>
                {
                    public void Configure(EntityTypeBuilder<Order> builder)
                    {
                        builder.HasKey(e => e.Id);
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0006");
    }

    #endregion

    #region SM0007: Duplicate entity configuration

    [Fact]
    public void SM0007_DuplicateEntityConfig_ReportsError()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Orders
            {
                public class Order { public int Id { get; set; } }

                [Module("Orders")]
                public class OrdersModule : IModule { }

                public class OrdersDbContext : DbContext
                {
                    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<Order> Orders => Set<Order>();
                }

                public class OrderConfigA : IEntityTypeConfiguration<Order>
                {
                    public void Configure(EntityTypeBuilder<Order> builder)
                    {
                        builder.HasKey(e => e.Id);
                    }
                }

                public class OrderConfigB : IEntityTypeConfiguration<Order>
                {
                    public void Configure(EntityTypeBuilder<Order> builder)
                    {
                        builder.Property(e => e.Id).ValueGeneratedNever();
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0007");
        var diag = diagnostics.First(d => d.Id == "SM0007");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("Order");
        message.Should().Contain("OrderConfigA");
        message.Should().Contain("OrderConfigB");

        // Should NOT emit HostDbContext when there are duplicate configs
        result
            .GeneratedTrees.Should()
            .NotContain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void SM0007_UniqueEntityConfigs_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Orders
            {
                public class Order { public int Id { get; set; } }
                public class OrderItem { public int ProductId { get; set; } }

                [Module("Orders")]
                public class OrdersModule : IModule { }

                public class OrdersDbContext : DbContext
                {
                    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<Order> Orders => Set<Order>();
                    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
                }

                public class OrderConfig : IEntityTypeConfiguration<Order>
                {
                    public void Configure(EntityTypeBuilder<Order> builder)
                    {
                        builder.HasKey(e => e.Id);
                    }
                }

                public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
                {
                    public void Configure(EntityTypeBuilder<OrderItem> builder)
                    {
                        builder.HasKey(e => e.ProductId);
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0007");
    }

    #endregion

    #region No false positives on valid code

    [Fact]
    public void ValidFullSetup_NoDiagnostics()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using Microsoft.AspNetCore.Identity;
            using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Users
            {
                public class AppUser : IdentityUser { }
                public class AppRole : IdentityRole { }

                [Module("Users")]
                public class UsersModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class UsersDbContext : IdentityDbContext<AppUser, AppRole, string>
                {
                    public UsersDbContext(DbContextOptions<UsersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }
            }

            namespace TestApp.Products
            {
                public class Product { public int Id { get; set; } }

                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class ProductsDbContext : DbContext
                {
                    public ProductsDbContext(DbContextOptions<ProductsDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<Product> Products => Set<Product>();
                }
            }

            namespace TestApp.Orders
            {
                public class Order { public int Id { get; set; } }
                public class OrderItem { public int ProductId { get; set; } }

                [Module("Orders")]
                public class OrdersModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class OrdersDbContext : DbContext
                {
                    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<Order> Orders => Set<Order>();
                    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
                }

                public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
                {
                    public void Configure(EntityTypeBuilder<OrderItem> builder)
                    {
                        builder.HasKey(e => e.ProductId);
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics
            .Where(d => d.Id.StartsWith("SM", StringComparison.Ordinal))
            .Should()
            .BeEmpty("valid setup should produce no SM diagnostics");
    }

    #endregion

    #region SM0002: Whitespace-only module name

    [Fact]
    public void SM0002_WhitespaceOnlyModuleName_DoesNotReport()
    {
        // Documents current behavior: IsNullOrEmpty does not catch whitespace-only names
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("   ")]
                public class WhitespaceModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics
            .Should()
            .NotContain(
                d => d.Id == "SM0002",
                "SM0002 uses IsNullOrEmpty, not IsNullOrWhiteSpace, so whitespace-only names are allowed"
            );
    }

    #endregion

    #region SM0003: Three IdentityDbContexts

    [Fact]
    public void SM0003_ThreeIdentityDbContexts_ReportsError()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.AspNetCore.Identity;
            using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Users
            {
                public class UserA : IdentityUser { }
                public class RoleA : IdentityRole { }

                [Module("Users")]
                public class UsersModule : IModule { }

                public class UsersDbContext : IdentityDbContext<UserA, RoleA, string>
                {
                    public UsersDbContext(DbContextOptions<UsersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }
            }

            namespace TestApp.Accounts
            {
                public class UserB : IdentityUser { }
                public class RoleB : IdentityRole { }

                [Module("Accounts")]
                public class AccountsModule : IModule { }

                public class AccountsDbContext : IdentityDbContext<UserB, RoleB, string>
                {
                    public AccountsDbContext(DbContextOptions<AccountsDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }
            }

            namespace TestApp.Auth
            {
                public class UserC : IdentityUser { }
                public class RoleC : IdentityRole { }

                [Module("Auth")]
                public class AuthModule : IModule { }

                public class AuthDbContext : IdentityDbContext<UserC, RoleC, string>
                {
                    public AuthDbContext(DbContextOptions<AuthDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0003");

        // Should NOT emit HostDbContext when there's a conflict
        result
            .GeneratedTrees.Should()
            .NotContain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));
    }

    #endregion

    #region SM0007: Three configs for same entity

    [Fact]
    public void SM0007_ThreeConfigsForSameEntity_ReportsError()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Orders
            {
                public class Order { public int Id { get; set; } }

                [Module("Orders")]
                public class OrdersModule : IModule { }

                public class OrdersDbContext : DbContext
                {
                    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<Order> Orders => Set<Order>();
                }

                public class OrderConfigA : IEntityTypeConfiguration<Order>
                {
                    public void Configure(EntityTypeBuilder<Order> builder)
                    {
                        builder.HasKey(e => e.Id);
                    }
                }

                public class OrderConfigB : IEntityTypeConfiguration<Order>
                {
                    public void Configure(EntityTypeBuilder<Order> builder)
                    {
                        builder.Property(e => e.Id).ValueGeneratedNever();
                    }
                }

                public class OrderConfigC : IEntityTypeConfiguration<Order>
                {
                    public void Configure(EntityTypeBuilder<Order> builder)
                    {
                        builder.Property(e => e.Id).HasColumnName("OrderId");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0007");
    }

    #endregion

    #region Overlapping module name prefixes

    [Fact]
    public void OverlappingModuleNamePrefixes_DbContextMatchedByLongestPrefix()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Product
            {
                public class ProductEntity { public int Id { get; set; } }

                [Module("Product")]
                public class ProductModule : IModule { }

                public class ProductDbContext : DbContext
                {
                    public ProductDbContext(DbContextOptions<ProductDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Product.ProductEntity> Products => Set<global::TestApp.Product.ProductEntity>();
                }
            }

            namespace TestApp.ProductInventory
            {
                public class InventoryItem { public int Id { get; set; } }

                [Module("ProductInventory")]
                public class ProductInventoryModule : IModule { }

                public class ProductInventoryDbContext : DbContext
                {
                    public ProductInventoryDbContext(DbContextOptions<ProductInventoryDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.ProductInventory.InventoryItem> InventoryItems => Set<global::TestApp.ProductInventory.InventoryItem>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // No SM diagnostics — each DbContext should match the correct module
        diagnostics
            .Where(d => d.Id.StartsWith("SM", StringComparison.Ordinal))
            .Should()
            .BeEmpty("overlapping prefixes should resolve correctly via longest match");

        // HostDbContext should be emitted
        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));

        // Both modules' DbSets should be present
        var hostDbContext = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        hostDbContext.Should().Contain("Products");
        hostDbContext.Should().Contain("InventoryItems");

        // Each module should get its own schema
        hostDbContext.Should().Contain("SetSchema(\"product\")");
        hostDbContext.Should().Contain("SetSchema(\"productinventory\")");
    }

    #endregion

    #region Mixed module types (Identity + Plain + NoDb)

    [Fact]
    public void MixedModuleTypes_IdentityPlainAndNoDb_EmitsCorrectHostDbContext()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.AspNetCore.Identity;
            using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Users
            {
                public class AppUser : IdentityUser { }
                public class AppRole : IdentityRole { }

                [Module("Users")]
                public class UsersModule : IModule { }

                public class UsersDbContext : IdentityDbContext<AppUser, AppRole, string>
                {
                    public UsersDbContext(DbContextOptions<UsersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }
                }
            }

            namespace TestApp.Products
            {
                public class Product { public int Id { get; set; } }

                [Module("Products")]
                public class ProductsModule : IModule { }

                public class ProductsDbContext : DbContext
                {
                    public ProductsDbContext(DbContextOptions<ProductsDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Products.Product> Products => Set<global::TestApp.Products.Product>();
                }
            }

            namespace TestApp.Notifications
            {
                [Module("Notifications")]
                public class NotificationsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration config) { }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // No SM diagnostics
        diagnostics
            .Where(d => d.Id.StartsWith("SM", StringComparison.Ordinal))
            .Should()
            .BeEmpty("mixed module types should produce no SM diagnostics");

        // HostDbContext should be emitted
        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));

        var hostDbContext = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        // Should extend IdentityDbContext (from Users module)
        hostDbContext.Should().Contain("IdentityDbContext<");

        // Should have Products DbSet from plain module
        hostDbContext.Should().Contain("Products");

        // NoDb module (Notifications) should not cause any issues
        hostDbContext.Should().NotContain("Notifications");
    }

    #endregion

    #region Deeply nested namespace matching

    [Fact]
    public void DeeplyNestedNamespace_DbContextMatchedCorrectly()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Company.Division.Orders
            {
                public class Order { public int Id { get; set; } }

                [Module("Orders")]
                public class OrdersModule : IModule { }
            }

            namespace TestApp.Company.Division.Orders.Infrastructure
            {
                public class OrdersDbContext : DbContext
                {
                    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Company.Division.Orders.Order> Orders => Set<global::TestApp.Company.Division.Orders.Order>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // No SM diagnostics
        diagnostics
            .Where(d => d.Id.StartsWith("SM", StringComparison.Ordinal))
            .Should()
            .BeEmpty("deeply nested namespace should match correctly");

        // HostDbContext should be emitted
        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));

        var hostDbContext = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        hostDbContext.Should().Contain("Orders");
        hostDbContext.Should().Contain("SetSchema(\"orders\")");
    }

    #endregion

    #region DbContext in unrelated namespace fallback

    [Fact]
    public void DbContextInUnrelatedNamespace_StillAssignedToModule()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Products
            {
                public class Product { public int Id { get; set; } }

                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace Unrelated.Namespace
            {
                public class SomeDbContext : DbContext
                {
                    public SomeDbContext(DbContextOptions<SomeDbContext> options, IOptions<DatabaseOptions> dbOptions)
                        : base(options) { }

                    public DbSet<global::TestApp.Products.Product> Products => Set<global::TestApp.Products.Product>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithEfCore(source);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // HostDbContext should still be emitted (DbContext is assigned via fallback)
        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal));

        var hostDbContext = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("HostDbContext.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        hostDbContext.Should().Contain("Products");
    }

    #endregion

    #region SM0015: Duplicate view page name

    [Fact]
    public void SM0015_DifferentModulesSameClassName_NoDiagnostic()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp.ModuleA
            {
                [Module("ModuleA", ViewPrefix = "/module-a")]
                public class ModuleAModule : IModule { }
            }

            namespace TestApp.ModuleA.Views
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }
            }

            namespace TestApp.ModuleB
            {
                [Module("ModuleB", ViewPrefix = "/module-b")]
                public class ModuleBModule : IModule { }
            }

            namespace TestApp.ModuleB.Views
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // "ModuleA/Browse" and "ModuleB/Browse" are different page names — no conflict
        diagnostics.Should().NotContain(d => d.Id == "SM0015");
    }

    [Fact]
    public void SM0015_UniquePageNames_NoDiagnostic()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
            }

            namespace TestApp.Views
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }

                public class CreateEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/create", () => "create");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0015");
    }

    #endregion

    #region SM0040: Duplicate module name

    [Fact]
    public void SM0040_DuplicateModuleName_ReportsError()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.ModuleA
            {
                [Module("Products")]
                public class ProductsModuleA : IModule { }
            }

            namespace TestApp.ModuleB
            {
                [Module("Products")]
                public class ProductsModuleB : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0040");
        var diag = diagnostics.First(d => d.Id == "SM0040");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("Products");
        message.Should().Contain("ProductsModuleA");
        message.Should().Contain("ProductsModuleB");
    }

    [Fact]
    public void SM0040_UniqueModuleNames_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.ModuleA
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.ModuleB
            {
                [Module("Orders")]
                public class OrdersModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0040");
    }

    [Fact]
    public void SM0040_ThreeModulesSameName_ReportsError()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.A { [Module("Catalog")] public class CatalogA : IModule { } }
            namespace TestApp.B { [Module("Catalog")] public class CatalogB : IModule { } }
            namespace TestApp.C { [Module("Catalog")] public class CatalogC : IModule { } }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0040");
        // At least two diagnostics (B conflicts with A, C conflicts with A)
        diagnostics.Where(d => d.Id == "SM0040").Should().HaveCountGreaterThanOrEqualTo(2);
    }

    #endregion

    #region SM0041: View page prefix mismatch

    [Fact]
    public void SM0041_PagePrefixMatchesModuleName_NoDiagnostic()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products", ViewPrefix = "/products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Views
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // Page name "Products/Browse" starts with "Products/" — no mismatch
        diagnostics.Should().NotContain(d => d.Id == "SM0041");
    }

    #endregion

    #region SM0042: Module with views but no ViewPrefix

    [Fact]
    public void SM0042_ViewEndpointWithoutViewPrefix_ReportsError()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Views
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0042");
        var diag = diagnostics.First(d => d.Id == "SM0042");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("Products");
        message.Should().Contain("/products");
    }

    [Fact]
    public void SM0042_ViewEndpointWithViewPrefix_NoDiagnostic()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products", ViewPrefix = "/products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Views
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0042");
    }

    [Fact]
    public void SM0042_NoViewEndpoints_NoDiagnostic()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Endpoints
            {
                public class ListEndpoint : IEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/", () => "list");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // No IViewEndpoint, so SM0042 should not fire even without ViewPrefix
        diagnostics.Should().NotContain(d => d.Id == "SM0042");
    }

    #endregion

    #region SM0055: Entity class must live in a Contracts assembly

    [Fact]
    public void SM0055_EntityInImplementationAssembly_ReportsError()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Products
            {
                // Entity declared in an implementation (non-Contracts) assembly.
                public class Product { public int Id { get; set; } }

                [Module("Products")]
                public class ProductsModule : IModule { }

                public class ProductsDbContext : DbContext
                {
                    public ProductsDbContext(
                        DbContextOptions<ProductsDbContext> options,
                        IOptions<DatabaseOptions> dbOptions
                    ) : base(options) { }

                    public DbSet<Product> Products => Set<Product>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateEfCoreCompilationWithAssemblyName(
            "SimpleModule.Products",
            source
        );
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0055");
        var diag = diagnostics.First(d => d.Id == "SM0055");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("Product");
        message.Should().Contain("Products");
        message.Should().Contain("SimpleModule.Products");
        message.Should().Contain("SimpleModule.Products.Contracts");
    }

    [Fact]
    public void SM0055_EntityInContractsAssembly_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Products
            {
                // Entity declared in the Contracts assembly — compliant.
                public class Product { public int Id { get; set; } }

                [Module("Products")]
                public class ProductsModule : IModule { }

                public class ProductsDbContext : DbContext
                {
                    public ProductsDbContext(
                        DbContextOptions<ProductsDbContext> options,
                        IOptions<DatabaseOptions> dbOptions
                    ) : base(options) { }

                    public DbSet<Product> Products => Set<Product>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateEfCoreCompilationWithAssemblyName(
            "SimpleModule.Products.Contracts",
            source
        );
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0055");
    }

    [Fact]
    public void SM0055_EntityInModuleSuffixedAssembly_SuggestsContractsSibling()
    {
        // When the implementation assembly has the '.Module' suffix (e.g.
        // SimpleModule.Agents.Module), the suggested replacement is the
        // '.Contracts' sibling (SimpleModule.Agents.Contracts), not a
        // double-suffixed '.Module.Contracts' assembly.
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Agents
            {
                public class AgentSession { public int Id { get; set; } }

                [Module("Agents")]
                public class AgentsModule : IModule { }

                public class AgentsDbContext : DbContext
                {
                    public AgentsDbContext(
                        DbContextOptions<AgentsDbContext> options,
                        IOptions<DatabaseOptions> dbOptions
                    ) : base(options) { }

                    public DbSet<AgentSession> Sessions => Set<AgentSession>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateEfCoreCompilationWithAssemblyName(
            "SimpleModule.Agents.Module",
            source
        );
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0055");
        var diag = diagnostics.First(d => d.Id == "SM0055");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("SimpleModule.Agents.Contracts");
        message.Should().NotContain("SimpleModule.Agents.Module.Contracts");
    }

    [Fact]
    public void SM0055_NonSimpleModuleAssembly_NoDiagnostic()
    {
        // External entities (e.g. OpenIddict) live outside SimpleModule.* and
        // cannot be moved by the author, so we don't flag them.
        var source = """
            using SimpleModule.Core;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;

            namespace TestApp.Widgets
            {
                public class Widget { public int Id { get; set; } }

                [Module("Widgets")]
                public class WidgetsModule : IModule { }

                public class WidgetsDbContext : DbContext
                {
                    public WidgetsDbContext(
                        DbContextOptions<WidgetsDbContext> options,
                        IOptions<DatabaseOptions> dbOptions
                    ) : base(options) { }

                    public DbSet<Widget> Widgets => Set<Widget>();
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateEfCoreCompilationWithAssemblyName(
            "Contoso.Widgets",
            source
        );
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0055");
    }

    #endregion
}
