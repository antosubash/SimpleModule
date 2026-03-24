using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Core.Events;

/// <summary>
/// Publishes events to registered handlers with exception isolation semantics.
/// </summary>
/// <remarks>
/// <para>
/// The EventBus is the central event distribution mechanism in the framework. It ensures reliable
/// event delivery with partial success guarantees: even if one handler fails, other handlers will
/// still execute. This prevents cascade failures across the application.
/// </para>
/// <para>
/// <strong>Execution Semantics:</strong>
/// <list type="bullet">
///   <item>
///     <description>
///       All registered handlers for an event execute sequentially in registration order,
///       regardless of failures.
///     </description>
///   </item>
///   <item>
///     <description>
///       If any handler throws an exception, it is caught and logged, then collected for later rethrow.
///     </description>
///   </item>
///   <item>
///     <description>
///       After all handlers have executed, if any exceptions were collected, they are thrown as
///       a single <see cref="AggregateException"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///       Handler failures are isolated: a throwing handler cannot prevent subsequent handlers from executing.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <strong>Handler Best Practices:</strong>
/// <list type="bullet">
///   <item>
///     <description>
///       Handlers should be independent and not rely on side effects from other handlers.
///     </description>
///   </item>
///   <item>
///     <description>
///       Handlers should not throw exceptions for expected failures; use result types or logging instead.
///     </description>
///   </item>
///   <item>
///     <description>
///       Handlers should be idempotent when possible, as the same event may be reprocessed in retry scenarios.
///     </description>
///   </item>
///   <item>
///     <description>
///       For long-running or critical side effects, consider using a persistent outbox pattern
///       or reliable message queue instead of synchronous event handlers.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <strong>Exception Handling:</strong>
/// All exceptions thrown by handlers are logged with handler name and event type. The caller
/// is responsible for handling <see cref="AggregateException"/> appropriately. If you need
/// to know which handlers failed, inspect the <see cref="AggregateException.InnerExceptions"/> collection.
/// </para>
/// </remarks>
public sealed partial class EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
    : IEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers, ensuring all handlers execute even if some fail.
    /// </summary>
    /// <typeparam name="T">The event type, must implement <see cref="IEvent"/>.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">
    /// A cancellation token that propagates cancellation requests to all handlers.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that completes when all handlers have executed.
    /// </returns>
    /// <exception cref="AggregateException">
    /// Thrown after all handlers have executed if any handler(s) threw an exception.
    /// The <see cref="AggregateException.InnerExceptions"/> collection contains all exceptions
    /// that were thrown by handlers.
    /// </exception>
    /// <remarks>
    /// Handler failures are isolated by design. If handler A throws, handler B will still execute.
    /// This method will not throw immediately when the first handler fails; instead, it collects
    /// all exceptions and throws them together as an AggregateException. This ensures that:
    /// <list type="number">
    ///   <item>
    ///     <description>All handlers have an opportunity to execute their work.</description>
    ///   </item>
    ///   <item>
    ///     <description>Side effects from successful handlers are preserved.</description>
    ///   </item>
    ///   <item>
    ///     <description>The caller can see all failures at once, enabling proper diagnostics.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<T>>();
        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken);
            }
#pragma warning disable CA1031 // Event bus must isolate handler failures
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogHandlerFailed(logger, handler.GetType().Name, typeof(T).Name, ex);
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(
                $"One or more event handlers for {typeof(T).Name} failed.",
                exceptions
            );
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Event handler {HandlerName} failed for event {EventName}"
    )]
    private static partial void LogHandlerFailed(
        ILogger logger,
        string handlerName,
        string eventName,
        Exception exception
    );
}
