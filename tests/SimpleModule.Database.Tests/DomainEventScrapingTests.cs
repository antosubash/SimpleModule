using JasperFx.Resources;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Entities;
using SimpleModule.Core.Events;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Sqlite;

namespace SimpleModule.Database.Tests;

/// <summary>
/// Verifies the new Wolverine scraper replacement for the deleted DomainEventInterceptor:
/// when a tracked entity implementing <see cref="IHasDomainEvents"/> is saved, the
/// configured selector flushes its events into Wolverine's outbox and a handler runs
/// on the other side. Also asserts the entity's event list is cleared so a second
/// SaveChanges of the same aggregate does not republish.
/// </summary>
public sealed class DomainEventScrapingTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"scraping-{Guid.NewGuid():N}.db"
    );

    private string ConnectionString => $"Data Source={_dbPath}";

    public void Dispose()
    {
        try
        {
            File.Delete(_dbPath);
        }
#pragma warning disable CA1031
        catch
        {
            // Best-effort cleanup.
        }
#pragma warning restore CA1031
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveChangesAndFlushMessages_On_Tracked_Aggregate_Publishes_Domain_Event()
    {
        ScrapeHandler.Reset();

        // Create the EF schema BEFORE Wolverine starts. EnsureCreated short-circuits
        // if the database already has any tables, so once Wolverine's resource setup
        // populates the file we can no longer create the aggregate table through EF.
        await CreateAggregateTableAsync();

        using var host = await BuildHostAsync();
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScrapingDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox>();

        var aggregate = new ScrapingAggregate { Id = 1, Name = "Test" };
        aggregate.Events.Add(new ScrapingUpdatedEvent("hello"));
        db.Aggregates.Add(aggregate);

        // The outbox pattern: enroll the DbContext, then flush both the EF write
        // and the outbox envelopes atomically. PublishDomainEventsFromEntityFrameworkCore
        // scrapes Events off tracked entities during this combined save.
        outbox.Enroll(db);
        await outbox.SaveChangesAndFlushMessagesAsync();

        await ScrapeHandler.InvokedTask.Task.WaitAsync(TimeSpan.FromSeconds(5));
        ScrapeHandler.LastPayload.Should().Be("hello");

        // Note: Wolverine reads the Events list but does NOT clear it. Aggregates
        // are typically loaded fresh from the DB per operation, so the in-memory
        // list is discarded with the instance. Code that re-uses the same instance
        // across multiple saves should call entity.Events.Clear() between saves.

        await host.StopAsync();
    }

    private async Task CreateAggregateTableAsync()
    {
        var opts = new DbContextOptionsBuilder<ScrapingDbContext>()
            .UseSqlite(ConnectionString)
            .Options;
        await using var seed = new ScrapingDbContext(opts);
        await seed.Database.EnsureCreatedAsync();
    }

    private async Task<IHost> BuildHostAsync()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddDbContext<ScrapingDbContext>(
            x => x.UseSqlite(ConnectionString),
            optionsLifetime: ServiceLifetime.Singleton
        );

        builder.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(DomainEventScrapingTests).Assembly);
            opts.PersistMessagesWithSqlite(ConnectionString);
            opts.UseEntityFrameworkCoreTransactions();
            opts.PublishDomainEventsFromEntityFrameworkCore<IHasDomainEvents>(x => x.Events);
            opts.Policies.UseDurableLocalQueues();
        });

        builder.Services.AddResourceSetupOnStartup();

        var host = builder.Build();
        await host.StartAsync();
        return host;
    }
}

public sealed record ScrapingUpdatedEvent(string Payload) : DomainEvent;

public sealed class ScrapingAggregate : IHasDomainEvents
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<IEvent> Events { get; } = [];
}

public sealed class ScrapingDbContext(DbContextOptions<ScrapingDbContext> options)
    : DbContext(options)
{
    public DbSet<ScrapingAggregate> Aggregates => Set<ScrapingAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScrapingAggregate>().HasKey(x => x.Id);
        modelBuilder.Entity<ScrapingAggregate>().Ignore(x => x.Events);
    }
}

public static class ScrapeHandler
{
    public static TaskCompletionSource InvokedTask { get; private set; } = new();
    public static string? LastPayload { get; private set; }

    public static void Reset()
    {
        InvokedTask = new TaskCompletionSource();
        LastPayload = null;
    }

    public static void Handle(ScrapingUpdatedEvent evt)
    {
        LastPayload = evt.Payload;
        InvokedTask.TrySetResult();
    }
}
