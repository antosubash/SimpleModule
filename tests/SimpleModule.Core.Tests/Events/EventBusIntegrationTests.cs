using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.Core.Events;

namespace SimpleModule.Core.Tests.Events;

/// <summary>
/// Integration tests verifying cross-module event flows work correctly
/// using the full DI container and event bus pipeline (handlers, pipeline behaviors,
/// background dispatch).
/// </summary>
public sealed class EventBusIntegrationTests
{
    private sealed record TestEvent(string Value) : IEvent;

    private sealed record AnotherEvent(int Number) : IEvent;

    #region Handlers

    private sealed class TrackingHandler : IEventHandler<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = [];
        public bool WasCalled => ReceivedEvents.Count > 0;

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            ReceivedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class SecondTrackingHandler : IEventHandler<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = [];
        public bool WasCalled => ReceivedEvents.Count > 0;

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            ReceivedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class ThirdTrackingHandler : IEventHandler<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = [];
        public bool WasCalled => ReceivedEvents.Count > 0;

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            ReceivedEvents.Add(@event);
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

    #endregion

    #region Pipeline Behaviors

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

    #endregion

    /// <summary>
    /// Creates a fully configured <see cref="ServiceProvider"/> with the EventBus,
    /// BackgroundEventChannel, and BackgroundEventDispatcher registered, plus any
    /// additional service registrations provided by the caller.
    /// </summary>
    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();

        // Core event infrastructure
        services.AddSingleton<BackgroundEventChannel>(sp => new BackgroundEventChannel(
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
        services.AddSingleton<IHostedService>(sp =>
            sp.GetRequiredService<BackgroundEventDispatcher>()
        );

        // Caller-specific registrations (handlers, behaviors, etc.)
        configure(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Event_PublishAsync_InvokesRegisteredHandler()
    {
        // Arrange
        var handler = new TrackingHandler();
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IEventHandler<TestEvent>>(handler);
        });
        var bus = provider.GetRequiredService<IEventBus>();
        var testEvent = new TestEvent("integration-test");

        // Act
        await bus.PublishAsync(testEvent);

        // Assert
        handler.WasCalled.Should().BeTrue();
        handler.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("integration-test");
    }

    [Fact]
    public async Task Event_PublishAsync_MultipleHandlers_AllInvoked()
    {
        // Arrange
        var handler1 = new TrackingHandler();
        var handler2 = new SecondTrackingHandler();
        var handler3 = new ThirdTrackingHandler();
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IEventHandler<TestEvent>>(handler1);
            services.AddSingleton<IEventHandler<TestEvent>>(handler2);
            services.AddSingleton<IEventHandler<TestEvent>>(handler3);
        });
        var bus = provider.GetRequiredService<IEventBus>();
        var testEvent = new TestEvent("multi-handler");

        // Act
        await bus.PublishAsync(testEvent);

        // Assert
        handler1.WasCalled.Should().BeTrue();
        handler1.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("multi-handler");

        handler2.WasCalled.Should().BeTrue();
        handler2.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("multi-handler");

        handler3.WasCalled.Should().BeTrue();
        handler3.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("multi-handler");
    }

    [Fact]
    public async Task Event_PublishAsync_HandlerThrows_OtherHandlersStillRun()
    {
        // Arrange: throwing handler sandwiched between two successful handlers
        var handlerBefore = new TrackingHandler();
        var throwingHandler = new ThrowingHandler();
        var handlerAfter = new SecondTrackingHandler();

        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IEventHandler<TestEvent>>(handlerBefore);
            services.AddSingleton<IEventHandler<TestEvent>>(throwingHandler);
            services.AddSingleton<IEventHandler<TestEvent>>(handlerAfter);
        });
        var bus = provider.GetRequiredService<IEventBus>();
        var testEvent = new TestEvent("partial-failure");

        // Act
        var act = () => bus.PublishAsync(testEvent);

        // Assert: AggregateException is thrown with the failing handler's exception
        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<InvalidOperationException>()
            .Which.Message.Should()
            .Be("Handler intentionally failed");

        // Both non-throwing handlers still executed
        handlerBefore.WasCalled.Should().BeTrue();
        handlerBefore
            .ReceivedEvents.Should()
            .ContainSingle()
            .Which.Value.Should()
            .Be("partial-failure");

        throwingHandler.WasCalled.Should().BeTrue();

        handlerAfter.WasCalled.Should().BeTrue();
        handlerAfter
            .ReceivedEvents.Should()
            .ContainSingle()
            .Which.Value.Should()
            .Be("partial-failure");
    }

    [Fact]
    public async Task Event_PublishInBackground_EventuallyInvokesHandler()
    {
        // Arrange
        var handler = new TrackingHandler();
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IEventHandler<TestEvent>>(handler);
        });
        var bus = provider.GetRequiredService<IEventBus>();
        var channel = provider.GetRequiredService<BackgroundEventChannel>();

        // Start the background dispatcher so it reads from the channel
        var dispatcher = provider.GetRequiredService<BackgroundEventDispatcher>();
        using var cts = new CancellationTokenSource();
        await dispatcher.StartAsync(cts.Token);

        try
        {
            var testEvent = new TestEvent("background-event");

            // Act: fire-and-forget publish
            bus.PublishInBackground(testEvent);

            // Assert: wait for the background dispatcher to process the event.
            // We poll briefly rather than sleeping a fixed duration to keep the test fast.
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!handler.WasCalled && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            handler
                .WasCalled.Should()
                .BeTrue("the background dispatcher should have invoked the handler");
            handler
                .ReceivedEvents.Should()
                .ContainSingle()
                .Which.Value.Should()
                .Be("background-event");
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
        // Arrange
        var behavior = new TrackingPipelineBehavior();
        var handler = new TrackingHandler();

        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IEventPipelineBehavior<TestEvent>>(behavior);
            services.AddSingleton<IEventHandler<TestEvent>>(handler);
        });
        var bus = provider.GetRequiredService<IEventBus>();
        var testEvent = new TestEvent("pipeline-test");

        // Act
        await bus.PublishAsync(testEvent);

        // Assert: the pipeline behavior was invoked and wrapped the handler
        behavior.BeforeHandlerCalled.Should().BeTrue();
        behavior.AfterHandlerCalled.Should().BeTrue();
        behavior.ReceivedEvent.Should().NotBeNull();
        behavior.ReceivedEvent!.Value.Should().Be("pipeline-test");

        // The handler was also called (the behavior called next())
        handler.WasCalled.Should().BeTrue();
        handler.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("pipeline-test");
    }
}
