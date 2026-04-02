using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Entities;
using SimpleModule.Database.Interceptors;

namespace SimpleModule.Database.Tests;

public sealed class EntityChangeInterceptorTests
{
    [Fact]
    public async Task Handler_Invoked_On_Entity_Add()
    {
        var handler = new TestChangeHandler();
        await using var fixture = CreateFixture(handler);

        fixture.Context.ChangeTrackedEntities.Add(new ChangeTrackedTestEntity { Name = "Test" });
        await fixture.Context.SaveChangesAsync();

        handler.Changes.Should().HaveCount(1);
        handler.Changes[0].ChangeType.Should().Be(EntityChangeType.Created);
        handler.Changes[0].Entity.Name.Should().Be("Test");
    }

    [Fact]
    public async Task Handler_Invoked_On_Entity_Update()
    {
        var handler = new TestChangeHandler();
        await using var fixture = CreateFixture(handler);

        var entity = new ChangeTrackedTestEntity { Name = "Test" };
        fixture.Context.ChangeTrackedEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();
        handler.Changes.Clear();

        entity.Name = "Updated";
        await fixture.Context.SaveChangesAsync();

        handler.Changes.Should().HaveCount(1);
        handler.Changes[0].ChangeType.Should().Be(EntityChangeType.Updated);
    }

    [Fact]
    public async Task Handler_Invoked_On_Entity_Delete()
    {
        var handler = new TestChangeHandler();
        await using var fixture = CreateFixture(handler);

        var entity = new ChangeTrackedTestEntity { Name = "Test" };
        fixture.Context.ChangeTrackedEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();
        handler.Changes.Clear();

        fixture.Context.ChangeTrackedEntities.Remove(entity);
        await fixture.Context.SaveChangesAsync();

        handler.Changes.Should().HaveCount(1);
        handler.Changes[0].ChangeType.Should().Be(EntityChangeType.Deleted);
    }

    [Fact]
    public async Task No_Handler_No_Errors()
    {
        await using var fixture = CreateFixture(handler: null);

        fixture.Context.ChangeTrackedEntities.Add(new ChangeTrackedTestEntity { Name = "Test" });
        var act = () => fixture.Context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    private static TestFixture CreateFixture(TestChangeHandler? handler)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Database:DefaultConnection"] = "Data Source=:memory:",
                }
            )
            .Build();

        var services = new ServiceCollection();

        if (handler is not null)
        {
            services.AddSingleton<IEntityChangeHandler<ChangeTrackedTestEntity>>(handler);
        }

        services.AddSingleton<ILogger<EntityChangeInterceptor>>(
            NullLogger<EntityChangeInterceptor>.Instance
        );
        services.AddScoped<ISaveChangesInterceptor, EntityChangeInterceptor>();
        services.AddModuleDbContext<ChangeTrackingTestDbContext>(config, "ChangeTrackingTest");

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<ChangeTrackingTestDbContext>();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return new TestFixture(provider, context);
    }

    private sealed class TestFixture(ServiceProvider provider, ChangeTrackingTestDbContext context)
        : IAsyncDisposable
    {
        public ChangeTrackingTestDbContext Context => context;

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await provider.DisposeAsync();
        }
    }
}

public class ChangeTrackedTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class TestChangeHandler : IEntityChangeHandler<ChangeTrackedTestEntity>
{
    public List<EntityChangeContext<ChangeTrackedTestEntity>> Changes { get; } = [];

    public Task HandleAsync(
        EntityChangeContext<ChangeTrackedTestEntity> context,
        CancellationToken cancellationToken = default
    )
    {
        Changes.Add(context);
        return Task.CompletedTask;
    }
}

public class ChangeTrackingTestDbContext(
    DbContextOptions<ChangeTrackingTestDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<ChangeTrackedTestEntity> ChangeTrackedEntities => Set<ChangeTrackedTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChangeTrackedTestEntity>(e => e.HasKey(x => x.Id));
        modelBuilder.ApplyModuleSchema("ChangeTrackingTest", dbOptions.Value);
    }
}
