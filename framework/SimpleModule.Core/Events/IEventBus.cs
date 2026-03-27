namespace SimpleModule.Core.Events;

/// <summary>
/// Publishes events to registered handlers with exception isolation semantics.
/// </summary>
/// <remarks>
/// The EventBus contract defines a single method for publishing events. Implementations must ensure
/// that handler failures are isolated and do not prevent other handlers from executing.
/// </remarks>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers.
    /// </summary>
    /// <typeparam name="T">The event type, must implement <see cref="IEvent"/>.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">
    /// A cancellation token that propagates cancellation requests to handlers.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that completes when all handlers have executed.
    /// </returns>
    /// <exception cref="AggregateException">
    /// Thrown after all handlers have executed if any handler(s) threw an exception.
    /// </exception>
    /// <remarks>
    /// Handlers are executed sequentially in registration order. Handler failures are isolated
    /// and do not prevent subsequent handlers from executing. If any handlers fail, an
    /// <see cref="AggregateException"/> is thrown after all handlers have run.
    /// </remarks>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent;

    /// <summary>
    /// Enqueues an event for background dispatch, returning immediately without waiting for handlers.
    /// </summary>
    /// <typeparam name="T">The event type, must implement <see cref="IEvent"/>.</typeparam>
    /// <param name="event">The event to publish in the background.</param>
    /// <remarks>
    /// The event is dispatched asynchronously by a background service. Handler exceptions are
    /// logged but do not propagate to the caller. Use this for fire-and-forget events where
    /// the caller does not need to know if handlers succeeded (e.g., audit logging, notifications).
    /// </remarks>
    void PublishInBackground<T>(T @event)
        where T : IEvent;
}
