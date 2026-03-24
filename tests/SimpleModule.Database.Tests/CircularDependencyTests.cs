using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.Database.Tests;

public sealed class CircularDependencyTests
{
    [Fact]
    public void AddModuleDbContext_WithInterceptorDependentOnDbContext_SuccessfullyCreatesContext()
    {
        var config = BuildConfig(
            new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "Data Source=:memory:",
            }
        );
        var services = new ServiceCollection();
        services.AddScoped<ISaveChangesInterceptor, CircularDependencyTestInterceptor>();
        services.AddScoped<ITestService, TestService>();
        services.AddModuleDbContext<CircularDependencyTestDbContext>(config, "TestModule");

        using var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<CircularDependencyTestDbContext>();
        context.Should().NotBeNull();
    }

    [Fact]
    public void MultipleInterceptors_AllResolveServicesAtInterceptionTime()
    {
        var config = BuildConfig(
            new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "Data Source=:memory:",
            }
        );
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestService>();
        services.AddScoped<ISaveChangesInterceptor, CircularDependencyTestInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, SecondTestInterceptor>();
        services.AddModuleDbContext<CircularDependencyTestDbContext>(config, "TestModule");

        using var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<CircularDependencyTestDbContext>();
        context.Should().NotBeNull();
    }

    [Fact]
    public void InterceptorCanOptionallyResolveServices()
    {
        var config = BuildConfig(
            new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "Data Source=:memory:",
            }
        );
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestService>();
        services.AddScoped<ISaveChangesInterceptor, CircularDependencyTestInterceptor>();
        services.AddModuleDbContext<CircularDependencyTestDbContext>(config, "TestModule");

        using var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<CircularDependencyTestDbContext>();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        context.Items.Add(new TestItem { Name = "Test" });
        context.SaveChanges();
        context.Should().NotBeNull();
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}

public class CircularDependencyTestDbContext(
    DbContextOptions<CircularDependencyTestDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<TestItem> Items => Set<TestItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });
        modelBuilder.ApplyModuleSchema("TestModule", dbOptions.Value);
    }
}

public class TestItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface ITestService
{
    void Call();
}

public class TestService(CircularDependencyTestDbContext dbContext) : ITestService
{
    public void Call()
    {
        _ = dbContext;
    }
}

public class CircularDependencyTestInterceptor(IServiceProvider? serviceProvider = null)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        var testService = serviceProvider?.GetService<ITestService>();
        testService?.Call();
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

public class SecondTestInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
