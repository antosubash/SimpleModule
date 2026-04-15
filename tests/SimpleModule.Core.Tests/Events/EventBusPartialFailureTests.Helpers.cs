using SimpleModule.Core.Events;

namespace SimpleModule.Core.Tests.Events;

public sealed partial class EventBusPartialFailureTests
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
