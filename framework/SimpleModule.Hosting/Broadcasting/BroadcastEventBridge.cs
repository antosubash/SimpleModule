using Microsoft.Extensions.Logging;
using SimpleModule.Core.Broadcasting;
using Wolverine;

namespace SimpleModule.Hosting.Broadcasting;

/// <summary>
/// Decorator over Wolverine's <see cref="IMessageBus"/> that mirrors every
/// <see cref="IBroadcastEvent"/> publication out to connected browsers via
/// <see cref="IBroadcaster"/>. Mirrors the pattern <c>AuditingMessageBus</c>
/// uses for audit capture so framework consumers see a single, consistent
/// extension point. Forwarding failures are logged but never propagated —
/// SignalR fan-out must not break the primary business operation that raised
/// the event.
/// </summary>
public sealed class BroadcastingMessageBus(
    IMessageBus inner,
    IBroadcaster broadcaster,
    ILogger<BroadcastingMessageBus>? logger = null
) : IMessageBus
{
    public string? TenantId
    {
        get => inner.TenantId;
        set => inner.TenantId = value;
    }

    public async ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
    {
        await inner.PublishAsync(message, options);
        if (message is IBroadcastEvent broadcast)
        {
            await ForwardAsync(broadcast);
        }
    }

    public async ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
    {
        await inner.SendAsync(message, options);
        if (message is IBroadcastEvent broadcast)
        {
            await ForwardAsync(broadcast);
        }
    }

    public async Task InvokeAsync(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        await inner.InvokeAsync(message, cancellation, timeout);
        if (message is IBroadcastEvent broadcast)
        {
            await ForwardAsync(broadcast, cancellation);
        }
    }

    public async Task InvokeAsync(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        await inner.InvokeAsync(message, options, cancellation, timeout);
        if (message is IBroadcastEvent broadcast)
        {
            await ForwardAsync(broadcast, cancellation);
        }
    }

    public async Task<T> InvokeAsync<T>(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        var result = await inner.InvokeAsync<T>(message, cancellation, timeout);
        if (message is IBroadcastEvent broadcast)
        {
            await ForwardAsync(broadcast, cancellation);
        }
        return result;
    }

    public async Task<T> InvokeAsync<T>(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        var result = await inner.InvokeAsync<T>(message, options, cancellation, timeout);
        if (message is IBroadcastEvent broadcast)
        {
            await ForwardAsync(broadcast, cancellation);
        }
        return result;
    }

    public Task InvokeForTenantAsync(
        string tenantId,
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => inner.InvokeForTenantAsync(tenantId, message, cancellation, timeout);

    public Task<T> InvokeForTenantAsync<T>(
        string tenantId,
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => inner.InvokeForTenantAsync<T>(tenantId, message, cancellation, timeout);

    public IDestinationEndpoint EndpointFor(string endpointName) => inner.EndpointFor(endpointName);

    public IDestinationEndpoint EndpointFor(Uri uri) => inner.EndpointFor(uri);

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message) =>
        inner.PreviewSubscriptions(message);

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options) =>
        inner.PreviewSubscriptions(message, options);

    public ValueTask BroadcastToTopicAsync(
        string topicName,
        object message,
        DeliveryOptions? options = null
    ) => inner.BroadcastToTopicAsync(topicName, message, options);

    private async Task ForwardAsync(
        IBroadcastEvent broadcast,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await broadcaster.PublishAsync(broadcast, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to forward broadcast event {EventType} to clients",
                broadcast.GetType().FullName
            );
        }
#pragma warning restore CA1031
    }
}
