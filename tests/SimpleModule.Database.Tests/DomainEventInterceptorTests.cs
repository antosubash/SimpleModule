using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Entities;
using SimpleModule.Core.Events;
using SimpleModule.Database.Interceptors;
using Wolverine;

namespace SimpleModule.Database.Tests;

public sealed class DomainEventInterceptorTests
{
    [Fact]
    public async Task Domain_Events_Dispatched_After_SaveChanges()
    {
        var bus = new RecordingMessageBus();
        await using var fixture = CreateFixture(bus);

        var entity = new AggregateRootTestEntity { Name = "Test" };
        entity.TriggerSomethingHappened();

        fixture.Context.AggregateRoots.Add(entity);
        await fixture.Context.SaveChangesAsync();

        bus.PublishedMessages.Should().HaveCount(1);
        bus.PublishedMessages[0].Should().BeOfType<SomethingHappenedEvent>();
    }

    [Fact]
    public async Task Domain_Events_Cleared_After_Dispatch()
    {
        var bus = new RecordingMessageBus();
        await using var fixture = CreateFixture(bus);

        var entity = new AggregateRootTestEntity { Name = "Test" };
        entity.TriggerSomethingHappened();

        fixture.Context.AggregateRoots.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task No_Events_Dispatched_When_No_Domain_Events()
    {
        var bus = new RecordingMessageBus();
        await using var fixture = CreateFixture(bus);

        var entity = new AggregateRootTestEntity { Name = "Test" };

        fixture.Context.AggregateRoots.Add(entity);
        await fixture.Context.SaveChangesAsync();

        bus.PublishedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Multiple_Events_From_Multiple_Entities_All_Dispatched()
    {
        var bus = new RecordingMessageBus();
        await using var fixture = CreateFixture(bus);

        var entity1 = new AggregateRootTestEntity { Name = "First" };
        entity1.TriggerSomethingHappened();
        entity1.TriggerSomethingHappened();

        var entity2 = new AggregateRootTestEntity { Name = "Second" };
        entity2.TriggerSomethingHappened();

        fixture.Context.AggregateRoots.AddRange(entity1, entity2);
        await fixture.Context.SaveChangesAsync();

        bus.PublishedMessages.Should().HaveCount(3);
    }

    private static TestFixture CreateFixture(IMessageBus bus)
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
        services.AddSingleton(bus);
        services.AddScoped<ISaveChangesInterceptor, DomainEventInterceptor>();
        services.AddModuleDbContext<DomainEventTestDbContext>(config, "DomainEventTest");

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<DomainEventTestDbContext>();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return new TestFixture(provider, context);
    }

    private sealed class TestFixture(ServiceProvider provider, DomainEventTestDbContext context)
        : IAsyncDisposable
    {
        public DomainEventTestDbContext Context => context;

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await provider.DisposeAsync();
        }
    }
}

public sealed record SomethingHappenedEvent : IEvent;

public class AggregateRootTestEntity : IHasDomainEvents
{
    private readonly List<IEvent> _events = [];

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<IEvent> GetDomainEvents() => _events.AsReadOnly();

    public void ClearDomainEvents() => _events.Clear();

    public void TriggerSomethingHappened() => _events.Add(new SomethingHappenedEvent());
}

/// <summary>
/// Recording IMessageBus that captures PublishAsync calls for assertion.
/// Other IMessageBus methods throw — the interceptor only uses PublishAsync.
/// </summary>
public sealed class RecordingMessageBus : IMessageBus
{
    public List<object> PublishedMessages { get; } = [];

    public string? TenantId { get; set; }

    public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
    {
        if (message is not null)
        {
            PublishedMessages.Add(message);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null) =>
        throw new NotImplementedException();

    public Task InvokeAsync(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public Task InvokeAsync(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public Task<T> InvokeAsync<T>(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public Task<T> InvokeAsync<T>(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public Task InvokeForTenantAsync(
        string tenantId,
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public Task<T> InvokeForTenantAsync<T>(
        string tenantId,
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public IDestinationEndpoint EndpointFor(string endpointName) =>
        throw new NotImplementedException();

    public IDestinationEndpoint EndpointFor(Uri uri) => throw new NotImplementedException();

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message) => [];

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options) =>
        [];

    public ValueTask BroadcastToTopicAsync(
        string topicName,
        object message,
        DeliveryOptions? options = null
    ) => throw new NotImplementedException();
}

public class DomainEventTestDbContext(
    DbContextOptions<DomainEventTestDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<AggregateRootTestEntity> AggregateRoots => Set<AggregateRootTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AggregateRootTestEntity>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.ApplyModuleSchema("DomainEventTest", dbOptions.Value);
    }
}
