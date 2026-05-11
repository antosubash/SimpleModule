using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleModule.Core.Broadcasting;
using SimpleModule.Hosting.Broadcasting;
using SimpleModule.Tests.Shared.Fixtures;
using Wolverine;

namespace SimpleModule.Core.Tests.Broadcasting;

/// <summary>
/// Verifies the broadcasting pieces are wired into the real test host:
/// the SignalR hub is registered, <see cref="IBroadcaster"/> resolves, and
/// the Wolverine bridge handles concrete <see cref="IBroadcastEvent"/>
/// subclasses via polymorphic dispatch. The handler dispatch is the load-
/// bearing assertion — without it, events would publish but never reach
/// browsers.
/// </summary>
[Collection(TestCollections.Integration)]
public sealed class BroadcasterIntegrationTests(SimpleModuleWebApplicationFactory factory)
{
    [Fact]
    public void Broadcaster_Is_Registered_In_DI()
    {
        // Boot the factory so all module services are wired.
        using var _ = factory.CreateClient();

        var broadcaster = factory.Services.GetRequiredService<IBroadcaster>();
        broadcaster.Should().BeOfType<Broadcaster>();

        // Hub must also resolve — proves AddSignalR ran.
        factory.Services.GetRequiredService<IHubContext<BroadcastHub>>().Should().NotBeNull();
    }

    [BroadcastEvent("test.broadcasting")]
    private sealed record TestBroadcastEvent(Guid Id) : IBroadcastEvent
    {
        public Guid EventId { get; } = Guid.CreateVersion7();
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

        public string Channel(IBroadcastContext context) => $"private-tests.{Id}";
    }

    [Fact]
    public async Task PublishingBus_Forwards_BroadcastEvents_To_The_Broadcaster()
    {
        // BroadcastingMessageBus decorates IMessageBus, so any IBroadcastEvent
        // published through the bus must reach the IBroadcaster — that's the
        // contract modules rely on without writing per-event handlers. A
        // per-test factory swaps in a recording broadcaster so the assertion
        // doesn't depend on the shared singleton from the collection fixture;
        // disposal is wrapped because Wolverine's shutdown of the inner host
        // race-cancels the durability-table SQL command on macOS occasionally.
        var recording = new RecordingBroadcaster();
        var local = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBroadcaster>();
                services.AddSingleton<IBroadcaster>(recording);
            })
        );
        try
        {
            using var _ = local.CreateClient();

            using var scope = local.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var evt = new TestBroadcastEvent(Guid.NewGuid());
            await bus.PublishAsync(evt);

            recording.Published.Should().ContainSingle().Which.Should().Be(evt);
        }
        finally
        {
            try
            {
                await local.DisposeAsync();
            }
#pragma warning disable CA1031
            catch
            {
                // Wolverine's StopAsync occasionally raises TaskCanceledException
                // when the durability tables are torn down concurrently. The
                // shared fixture swallows the same noise — see
                // SimpleModuleWebApplicationFactory.Dispose.
            }
#pragma warning restore CA1031
        }
    }
}

/// <summary>
/// Captures every <see cref="IBroadcastEvent"/> the bus forwards, so the
/// integration test can assert on it without standing up a real SignalR
/// connection.
/// </summary>
internal sealed class RecordingBroadcaster : IBroadcaster
{
    private readonly List<IBroadcastEvent> _events = new();

    public IReadOnlyList<IBroadcastEvent> Published
    {
        get
        {
            lock (_events)
            {
                return _events.ToList();
            }
        }
    }

    public Task PublishAsync(
        IBroadcastEvent broadcastEvent,
        CancellationToken cancellationToken = default
    )
    {
        lock (_events)
        {
            _events.Add(broadcastEvent);
        }
        return Task.CompletedTask;
    }

    public Task ToChannelAsync(
        string channel,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;

    public Task ToUserAsync(
        string userId,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;

    public Task ToTenantAsync(
        string tenantId,
        string @event,
        object payload,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;
}
