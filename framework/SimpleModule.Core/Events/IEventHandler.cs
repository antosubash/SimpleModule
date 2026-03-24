namespace SimpleModule.Core.Events;

/// <summary>
/// Handles an event of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The event type to handle.</typeparam>
/// <remarks>
/// <para>
/// Event handlers are invoked by the <see cref="IEventBus"/> when a matching event is published.
/// Multiple handlers can be registered for the same event type, and all will be invoked in
/// registration order.
/// </para>
/// <para>
/// <strong>Handler Implementation Guidelines:</strong>
/// <list type="bullet">
///   <item>
///     <description>
///       Handlers should be stateless and reusable. Avoid storing mutable state that could be
///       affected by concurrent or repeated invocations.
///     </description>
///   </item>
///   <item>
///     <description>
///       Handlers should not throw exceptions for expected failures. Use result types, early returns,
///       or logging instead. Exceptions interrupt the handler chain and must be handled by the caller.
///     </description>
///   </item>
///   <item>
///     <description>
///       Handlers are invoked sequentially in registration order. Do not rely on the execution order
///       of sibling handlers, but you can rely on handlers registered before yours having already executed.
///     </description>
///   </item>
///   <item>
///     <description>
///       If a handler needs to be transactional or have strong consistency guarantees, consider
///       registering the event handler in a database transaction or using compensating actions.
///     </description>
///   </item>
///   <item>
///     <description>
///       For long-running operations, consider using a background job queue or service instead of
///       synchronous event handlers.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <strong>Exception Behavior:</strong>
/// If a handler throws an exception, the <see cref="IEventBus"/> catches and logs it, then continues
/// invoking remaining handlers. After all handlers have executed, any collected exceptions are thrown
/// as an <see cref="AggregateException"/>. This design ensures partial success: even if one handler fails,
/// others complete their work.
/// </para>
/// </remarks>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - EventHandler is intentional
public interface IEventHandler<in T>
    where T : IEvent
{
    /// <summary>
    /// Handles the specified event.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">A cancellation token to observe during handler execution.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is invoked asynchronously by the <see cref="IEventBus"/>. If this method throws
    /// an exception, it will be caught, logged, and collected with other exceptions. The event bus
    /// will continue invoking remaining handlers even if this handler fails. After all handlers have
    /// executed, any collected exceptions will be thrown as an <see cref="AggregateException"/>.
    /// </remarks>
    Task HandleAsync(T @event, CancellationToken cancellationToken);
}
#pragma warning restore CA1711
