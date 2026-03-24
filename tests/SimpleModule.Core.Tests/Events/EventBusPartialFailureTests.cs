using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.Core.Events;

namespace SimpleModule.Core.Tests.Events;

/// <summary>
/// Comprehensive tests for EventBus exception isolation and partial failure handling.
/// These tests verify that the EventBus correctly implements the partial success semantics:
/// all handlers execute even if some fail, and failures are collected and rethrown together.
/// </summary>
public sealed class EventBusPartialFailureTests
{
    private sealed record TestEvent(string Value) : IEvent;

    /// <summary>
    /// Test handler that tracks successful execution.
    /// </summary>
    private sealed class SuccessfulHandler : IEventHandler<TestEvent>
    {
        public bool Executed { get; private set; }
        public TestEvent? ReceivedEvent { get; private set; }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            ReceivedEvent = @event;
            Executed = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Test handler that throws a specific exception.
    /// </summary>
    private sealed class FailingHandler : IEventHandler<TestEvent>
    {
        private readonly Exception _exception;

        public FailingHandler(Exception? exception = null)
        {
            _exception = exception ?? new InvalidOperationException("Handler failed");
        }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }

    /// <summary>
    /// Test handler that tracks execution order using a shared list.
    /// </summary>
    private sealed class OrderTrackingHandler : IEventHandler<TestEvent>
    {
        private readonly List<string> _executionOrder;
        private readonly string _handlerId;
        private readonly Exception? _exception;

        public OrderTrackingHandler(
            List<string> executionOrder,
            string handlerId,
            Exception? exception = null
        )
        {
            _executionOrder = executionOrder;
            _handlerId = handlerId;
            _exception = exception;
        }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            _executionOrder.Add(_handlerId);
            return _exception != null ? Task.FromException(_exception) : Task.CompletedTask;
        }
    }

    /// <summary>
    /// Test: When one handler throws, other handlers still execute.
    /// This is the core partial failure guarantee.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenOneHandlerFails_OtherHandlersStillExecute()
    {
        var successHandler1 = new SuccessfulHandler();
        var failingHandler = new FailingHandler(new InvalidOperationException("Handler 2 failed"));
        var successHandler2 = new SuccessfulHandler();

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(successHandler1);
        services.AddSingleton<IEventHandler<TestEvent>>(failingHandler);
        services.AddSingleton<IEventHandler<TestEvent>>(successHandler2);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var testEvent = new TestEvent("test-value");
        var act = async () => await bus.PublishAsync(testEvent);

        // AggregateException should be thrown
        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(1);

        // But both successful handlers should have executed despite the failure
        successHandler1.Executed.Should().BeTrue();
        successHandler1.ReceivedEvent.Should().BeEquivalentTo(testEvent);
        successHandler2.Executed.Should().BeTrue();
        successHandler2.ReceivedEvent.Should().BeEquivalentTo(testEvent);
    }

    /// <summary>
    /// Test: Exceptions from multiple failing handlers are all collected and thrown together.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenMultipleHandlersFail_AllExceptionsAreCollected()
    {
        var exception1 = new InvalidOperationException("Handler 1 error");
        var exception2 = new ArgumentException("Handler 2 error");
        var exception3 = new NotImplementedException("Handler 3 error");

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(new FailingHandler(exception1));
        services.AddSingleton<IEventHandler<TestEvent>>(new FailingHandler(exception2));
        services.AddSingleton<IEventHandler<TestEvent>>(new FailingHandler(exception3));
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(3);
        ex.Which.InnerExceptions.Should().Contain(exception1);
        ex.Which.InnerExceptions.Should().Contain(exception2);
        ex.Which.InnerExceptions.Should().Contain(exception3);
    }

    /// <summary>
    /// Test: Handlers execute in deterministic order (registration order).
    /// Even when some fail, the order is preserved.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ExecutionOrder_IsDeterministic_RegistrationOrder()
    {
        var executionOrder = new List<string>();
        var services = new ServiceCollection();

        // Register handlers in a specific order
        services.AddSingleton<IEventHandler<TestEvent>>(
            new OrderTrackingHandler(executionOrder, "Handler-1")
        );
        services.AddSingleton<IEventHandler<TestEvent>>(
            new OrderTrackingHandler(executionOrder, "Handler-2")
        );
        services.AddSingleton<IEventHandler<TestEvent>>(
            new OrderTrackingHandler(executionOrder, "Handler-3")
        );

        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        await bus.PublishAsync(new TestEvent("test"));

        // Handlers should execute in registration order
        executionOrder.Should().ContainInOrder("Handler-1", "Handler-2", "Handler-3");
    }

    /// <summary>
    /// Test: Handler execution order is preserved even when some handlers fail.
    /// A failing handler in the middle doesn't disrupt the sequence.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ExecutionOrder_PreservedEvenWhenSomeFail()
    {
        var executionOrder = new List<string>();
        var exception = new InvalidOperationException("Intentional failure");
        var services = new ServiceCollection();

        services.AddSingleton<IEventHandler<TestEvent>>(
            new OrderTrackingHandler(executionOrder, "Handler-1")
        );
        services.AddSingleton<IEventHandler<TestEvent>>(
            new OrderTrackingHandler(executionOrder, "Handler-2", exception)
        );
        services.AddSingleton<IEventHandler<TestEvent>>(
            new OrderTrackingHandler(executionOrder, "Handler-3")
        );
        services.AddSingleton<IEventHandler<TestEvent>>(
            new OrderTrackingHandler(executionOrder, "Handler-4")
        );

        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        // Should throw but handlers should still execute in order
        await act.Should().ThrowAsync<AggregateException>();
        executionOrder.Should().ContainInOrder("Handler-1", "Handler-2", "Handler-3", "Handler-4");
    }

    /// <summary>
    /// Test: Successful handlers complete their work even if later handlers fail.
    /// This verifies that side effects from successful handlers are preserved.
    /// </summary>
    [Fact]
    public async Task PublishAsync_SuccessfulHandlers_CompleteSideEffects_DespiteLaterFailures()
    {
        var completedWork = new List<string>();

        var successHandler1 = new OrderTrackingHandler(completedWork, "Work-1");
        var failingHandler = new FailingHandler(new InvalidOperationException("Failure"));
        var successHandler2 = new OrderTrackingHandler(completedWork, "Work-2");
        var successHandler3 = new OrderTrackingHandler(completedWork, "Work-3");

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(successHandler1);
        services.AddSingleton<IEventHandler<TestEvent>>(failingHandler);
        services.AddSingleton<IEventHandler<TestEvent>>(successHandler2);
        services.AddSingleton<IEventHandler<TestEvent>>(successHandler3);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        // Should throw due to the failing handler
        await act.Should().ThrowAsync<AggregateException>();

        // But all successful handlers should have completed their work
        completedWork.Should().ContainInOrder("Work-1", "Work-2", "Work-3");
    }

    /// <summary>
    /// Test: AggregateException message is informative and includes event type.
    /// </summary>
    [Fact]
    public async Task PublishAsync_AggregateException_IncludesEventTypeName()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(new FailingHandler());
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.Message.Should().Contain("TestEvent");
    }

    /// <summary>
    /// Test: CancellationToken is propagated to all handlers.
    /// This ensures handlers can respond to cancellation requests.
    /// </summary>
    [Fact]
    public async Task PublishAsync_CancellationToken_IsPropagatedToAllHandlers()
    {
        var receivedTokens = new List<CancellationToken>();

        var handler1 = new DelegateHandler(
            (_, ct) =>
            {
                receivedTokens.Add(ct);
                return Task.CompletedTask;
            }
        );
        var handler2 = new DelegateHandler(
            (_, ct) =>
            {
                receivedTokens.Add(ct);
                return Task.CompletedTask;
            }
        );

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        using var cts = new CancellationTokenSource();
        await bus.PublishAsync(new TestEvent("test"), cts.Token);

        receivedTokens.Should().HaveCount(2);
        receivedTokens.Should().AllSatisfy(t => t.Should().Be(cts.Token));
    }

    /// <summary>
    /// Test: CancellationToken is propagated even when handlers fail.
    /// </summary>
    [Fact]
    public async Task PublishAsync_CancellationToken_PropagatedEvenWhenHandlersFail()
    {
        var receivedToken = default(CancellationToken);
        var handler = new DelegateHandler(
            (_, ct) =>
            {
                receivedToken = ct;
                return Task.CompletedTask;
            }
        );

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        services.AddSingleton<IEventHandler<TestEvent>>(new FailingHandler());
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        using var cts = new CancellationTokenSource();

        var act = async () => await bus.PublishAsync(new TestEvent("test"), cts.Token);
        await act.Should().ThrowAsync<AggregateException>();

        receivedToken.Should().Be(cts.Token);
    }

    /// <summary>
    /// Test: When no handlers fail, no exception is thrown.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenAllHandlersSucceed_DoesNotThrow()
    {
        var handler1 = new SuccessfulHandler();
        var handler2 = new SuccessfulHandler();

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        await act.Should().NotThrowAsync();
        handler1.Executed.Should().BeTrue();
        handler2.Executed.Should().BeTrue();
    }

    /// <summary>
    /// Test: When no handlers are registered, no exception is thrown.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenNoHandlersRegistered_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider, NullLogger<EventBus>.Instance);

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Helper handler that delegates to a provided function.
    /// Used for testing token and event propagation.
    /// </summary>
    private sealed class DelegateHandler(Func<TestEvent, CancellationToken, Task> handler)
        : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken) =>
            handler(@event, cancellationToken);
    }
}
