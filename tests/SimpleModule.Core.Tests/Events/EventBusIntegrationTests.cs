using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.Core.Events;

namespace SimpleModule.Core.Tests.Events;

/// <summary>
/// Integration tests verifying cross-module event flows work correctly using the
/// full DI container and event bus pipeline (handlers, pipeline behaviors, background
/// dispatch).
/// </summary>
public sealed class EventBusIntegrationTests
{
    private sealed record TestEvent(string Value) : IEvent;

    // Distinct marker types let DI register three independent handler instances
    // without copy-pasting three identical handler classes.
    private interface IHandlerSlot
    {
        List<TestEvent> ReceivedEvents { get; }
    }

    private sealed class SlotOne : IHandlerSlot
    {
        public List<TestEvent> ReceivedEvents { get; } = [];
    }

    private sealed class SlotTwo : IHandlerSlot
    {
        public List<TestEvent> ReceivedEvents { get; } = [];
    }

    private sealed class SlotThree : IHandlerSlot
    {
        public List<TestEvent> ReceivedEvents { get; } = [];
    }

    private sealed class TrackingHandler<TSlot>(TSlot slot) : IEventHandler<TestEvent>
        where TSlot : IHandlerSlot
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            slot.ReceivedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IEventHandler<TestEvent>
    {
        public bool WasCalled { get; private set; }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            WasCalled = true;
            throw new InvalidOperationException("Handler intentionally failed");
        }
    }

    private sealed class SignallingHandler(TaskCompletionSource<TestEvent> tcs)
        : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            tcs.TrySetResult(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingPipelineBehavior : IEventPipelineBehavior<TestEvent>
    {
        public bool BeforeHandlerCalled { get; private set; }
        public bool AfterHandlerCalled { get; private set; }
        public TestEvent? ReceivedEvent { get; private set; }

        public async Task HandleAsync(
            TestEvent @event,
            Func<Task> next,
            CancellationToken cancellationToken
        )
        {
            ReceivedEvent = @event;
            BeforeHandlerCalled = true;
            await next();
            AfterHandlerCalled = true;
        }
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();

        services.AddSingleton<BackgroundEventChannel>(_ => new BackgroundEventChannel(
            NullLogger<BackgroundEventChannel>.Instance
        ));
        services.AddSingleton<IEventBus>(sp => new EventBus(
            sp,
            NullLogger<EventBus>.Instance,
            sp.GetRequiredService<BackgroundEventChannel>()
        ));
        services.AddSingleton<BackgroundEventDispatcher>(sp => new BackgroundEventDispatcher(
            sp.GetRequiredService<BackgroundEventChannel>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BackgroundEventDispatcher>.Instance
        ));

        configure(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Event_PublishAsync_InvokesRegisteredHandler()
    {
        var slot = new SlotOne();
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton(slot);
            services.AddSingleton<IEventHandler<TestEvent>, TrackingHandler<SlotOne>>();
        });
        var bus = provider.GetRequiredService<IEventBus>();

        await bus.PublishAsync(new TestEvent("integration-test"));

        slot.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("integration-test");
    }

    [Fact]
    public async Task Event_PublishAsync_MultipleHandlers_AllInvoked()
    {
        var slot1 = new SlotOne();
        var slot2 = new SlotTwo();
        var slot3 = new SlotThree();
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton(slot1);
            services.AddSingleton(slot2);
            services.AddSingleton(slot3);
            services.AddSingleton<IEventHandler<TestEvent>, TrackingHandler<SlotOne>>();
            services.AddSingleton<IEventHandler<TestEvent>, TrackingHandler<SlotTwo>>();
            services.AddSingleton<IEventHandler<TestEvent>, TrackingHandler<SlotThree>>();
        });
        var bus = provider.GetRequiredService<IEventBus>();

        await bus.PublishAsync(new TestEvent("multi-handler"));

        slot1.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("multi-handler");
        slot2.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("multi-handler");
        slot3.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("multi-handler");
    }

    [Fact]
    public async Task Event_PublishAsync_HandlerThrows_OtherHandlersStillRun()
    {
        var slotBefore = new SlotOne();
        var slotAfter = new SlotTwo();
        var throwingHandler = new ThrowingHandler();

        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton(slotBefore);
            services.AddSingleton(slotAfter);
            services.AddSingleton<IEventHandler<TestEvent>, TrackingHandler<SlotOne>>();
            services.AddSingleton<IEventHandler<TestEvent>>(throwingHandler);
            services.AddSingleton<IEventHandler<TestEvent>, TrackingHandler<SlotTwo>>();
        });
        var bus = provider.GetRequiredService<IEventBus>();

        var act = () => bus.PublishAsync(new TestEvent("partial-failure"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<InvalidOperationException>()
            .Which.Message.Should()
            .Be("Handler intentionally failed");

        slotBefore
            .ReceivedEvents.Should()
            .ContainSingle()
            .Which.Value.Should()
            .Be("partial-failure");
        throwingHandler.WasCalled.Should().BeTrue();
        slotAfter
            .ReceivedEvents.Should()
            .ContainSingle()
            .Which.Value.Should()
            .Be("partial-failure");
    }

    [Fact]
    public async Task Event_PublishInBackground_EventuallyInvokesHandler()
    {
        var tcs = new TaskCompletionSource<TestEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton(tcs);
            services.AddSingleton<IEventHandler<TestEvent>, SignallingHandler>();
        });
        var bus = provider.GetRequiredService<IEventBus>();
        var dispatcher = provider.GetRequiredService<BackgroundEventDispatcher>();
        using var cts = new CancellationTokenSource();
        await dispatcher.StartAsync(cts.Token);

        try
        {
            bus.PublishInBackground(new TestEvent("background-event"));

            var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            received.Value.Should().Be("background-event");
        }
        finally
        {
            await cts.CancelAsync();
            await dispatcher.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Event_PipelineBehavior_WrapsHandlerExecution()
    {
        var behavior = new TrackingPipelineBehavior();
        var slot = new SlotOne();

        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IEventPipelineBehavior<TestEvent>>(behavior);
            services.AddSingleton(slot);
            services.AddSingleton<IEventHandler<TestEvent>, TrackingHandler<SlotOne>>();
        });
        var bus = provider.GetRequiredService<IEventBus>();

        await bus.PublishAsync(new TestEvent("pipeline-test"));

        behavior.BeforeHandlerCalled.Should().BeTrue();
        behavior.AfterHandlerCalled.Should().BeTrue();
        behavior.ReceivedEvent.Should().NotBeNull();
        behavior.ReceivedEvent!.Value.Should().Be("pipeline-test");
        slot.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("pipeline-test");
    }
}
