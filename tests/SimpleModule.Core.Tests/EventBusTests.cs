using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.Core.Events;

namespace SimpleModule.Core.Tests;

public sealed class EventBusTests
{
    private sealed record TestEvent(string Value) : IEvent;

    private sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = [];

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            ReceivedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEventHandler : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Handler failed");
    }

    private sealed class OrderTrackingHandler : IEventHandler<TestEvent>
    {
        public List<int> CallOrder { get; }
        public int Id { get; }

        public OrderTrackingHandler(List<int> callOrder, int id)
        {
            CallOrder = callOrder;
            Id = id;
        }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            CallOrder.Add(Id);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PublishAsync_WithRegisteredHandler_InvokesHandler()
    {
        var handler = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        await bus.PublishAsync(new TestEvent("test"));

        handler.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("test");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_InvokesAll()
    {
        var handler1 = new TestEventHandler();
        var handler2 = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        await bus.PublishAsync(new TestEvent("test"));

        handler1.ReceivedEvents.Should().ContainSingle();
        handler2.ReceivedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task PublishAsync_WithNoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = () => bus.PublishAsync(new TestEvent("test"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerThrows_OtherHandlersStillExecute()
    {
        var successHandler = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(new ThrowingEventHandler());
        services.AddSingleton<IEventHandler<TestEvent>>(successHandler);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = () => bus.PublishAsync(new TestEvent("test"));

        await act.Should().ThrowAsync<AggregateException>();
        // The second handler should still have been called
        successHandler.ReceivedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerThrows_ThrowsAggregateException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(new ThrowingEventHandler());
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = () => bus.PublishAsync(new TestEvent("test"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().ContainSingle();
        ex.Which.InnerExceptions[0].Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task PublishAsync_WhenMultipleHandlersThrow_AggregatesAllExceptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(new ThrowingEventHandler());
        services.AddSingleton<IEventHandler<TestEvent>>(new ThrowingEventHandler());
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = () => bus.PublishAsync(new TestEvent("test"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAsync_HandlersExecuteInRegistrationOrder()
    {
        var callOrder = new List<int>();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(new OrderTrackingHandler(callOrder, 1));
        services.AddSingleton<IEventHandler<TestEvent>>(new OrderTrackingHandler(callOrder, 2));
        services.AddSingleton<IEventHandler<TestEvent>>(new OrderTrackingHandler(callOrder, 3));
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        await bus.PublishAsync(new TestEvent("test"));

        callOrder.Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public async Task PublishAsync_PassesCancellationToken_ToHandlers()
    {
        CancellationToken receivedToken = default;
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(
            new DelegateHandler(
                (_, ct) =>
                {
                    receivedToken = ct;
                    return Task.CompletedTask;
                }
            )
        );
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);
        using var cts = new CancellationTokenSource();

        await bus.PublishAsync(new TestEvent("test"), cts.Token);

        receivedToken.Should().Be(cts.Token);
    }

    private sealed class DelegateHandler(Func<TestEvent, CancellationToken, Task> handler)
        : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken) =>
            handler(@event, cancellationToken);
    }
}
