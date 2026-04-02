namespace SimpleModule.Core.Events;

/// <summary>
/// Defines a pipeline behavior that wraps event handler execution, enabling
/// cross-cutting concerns like logging, metrics, retries, or transaction boundaries.
/// </summary>
/// <typeparam name="T">The event type to intercept.</typeparam>
/// <remarks>
/// Pipeline behaviors execute in reverse registration order (outermost first),
/// forming a middleware chain around the event handler invocations.
/// Register via <c>services.AddScoped&lt;IEventPipelineBehavior&lt;MyEvent&gt;, MyBehavior&gt;()</c>.
/// </remarks>
/// <example>
/// <code>
/// public sealed class LoggingBehavior&lt;T&gt; : IEventPipelineBehavior&lt;T&gt; where T : IEvent
/// {
///     public async Task HandleAsync(T @event, Func&lt;Task&gt; next, CancellationToken ct)
///     {
///         _logger.LogInformation("Handling {Event}", typeof(T).Name);
///         await next();
///         _logger.LogInformation("Handled {Event}", typeof(T).Name);
///     }
/// }
/// </code>
/// </example>
public interface IEventPipelineBehavior<in T>
    where T : IEvent
{
    Task HandleAsync(T @event, Func<Task> next, CancellationToken cancellationToken);
}
