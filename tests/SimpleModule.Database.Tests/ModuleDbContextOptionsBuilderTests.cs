using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.Database.Tests;

public sealed class ModuleDbContextOptionsBuilderTests
{
    [Fact]
    public void AddModuleDbContext_WithModuleConnection_UsesModuleConnectionString()
    {
        var config = BuildConfig(
            new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "Data Source=shared.db",
                ["Database:ModuleConnections:TestModule"] = "Data Source=test.db",
            }
        );
        var services = new ServiceCollection();

        services.AddModuleDbContext<PerModuleDbContext>(config, "TestModule");
        using var provider = services.BuildServiceProvider();

        var dbContext = provider.GetRequiredService<PerModuleDbContext>();
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddModuleDbContext_WithoutModuleConnection_FallsBackToDefault()
    {
        var config = BuildConfig(
            new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "Data Source=shared.db",
            }
        );
        var services = new ServiceCollection();

        services.AddModuleDbContext<SharedSqliteDbContext>(config, "TestModule");
        using var provider = services.BuildServiceProvider();

        var dbContext = provider.GetRequiredService<SharedSqliteDbContext>();
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddModuleDbContext_RegistersDatabaseOptions()
    {
        var config = BuildConfig(
            new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "Data Source=shared.db",
                ["Database:ModuleConnections:TestModule"] = "Data Source=test.db",
            }
        );
        var services = new ServiceCollection();

        services.AddModuleDbContext<PerModuleDbContext>(config, "TestModule");
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>();
        options.Value.DefaultConnection.Should().Be("Data Source=shared.db");
        options.Value.ModuleConnections.Should().ContainKey("TestModule");
    }

    [Fact]
    public void AddModuleDbContext_RegistersModuleDbContextInfo()
    {
        var config = BuildConfig(
            new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "Data Source=shared.db",
            }
        );
        var services = new ServiceCollection();

        services.AddModuleDbContext<SharedSqliteDbContext>(config, "TestModule");
        using var provider = services.BuildServiceProvider();

        var infos = provider.GetServices<ModuleDbContextInfo>().ToList();
        infos
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new ModuleDbContextInfo("TestModule", typeof(SharedSqliteDbContext)));
    }

    [Fact]
    public void AddModuleDbContext_MultipleModules_RegistersAllInfos()
    {
        var config = BuildConfig(
            new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "Data Source=shared.db",
            }
        );
        var services = new ServiceCollection();

        services.AddModuleDbContext<SharedSqliteDbContext>(config, "Module1");
        services.AddModuleDbContext<Module2DbContext>(config, "Module2");
        using var provider = services.BuildServiceProvider();

        var infos = provider.GetServices<ModuleDbContextInfo>().ToList();
        infos.Should().HaveCount(2);
        infos.Select(i => i.ModuleName).Should().BeEquivalentTo("Module1", "Module2");
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}

public class Module2DbContext(
    DbContextOptions<Module2DbContext> options,
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

        modelBuilder.ApplyModuleSchema("Module2", dbOptions.Value);
    }
}
