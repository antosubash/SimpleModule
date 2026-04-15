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
public sealed partial class EventBusPartialFailureTests
{
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
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

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
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

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
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

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
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

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
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

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
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.Message.Should().Contain("TestEvent");
    }
}
