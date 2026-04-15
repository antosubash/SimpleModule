using Wolverine;

namespace SimpleModule.Tests.Shared.Fakes;

/// <summary>
/// Recording <see cref="IMessageBus"/> stub for unit tests. Captures every
/// message published, sent, or invoked in <see cref="PublishedEvents"/> so
/// tests can assert on publishing behaviour without wiring up Wolverine.
/// Methods unrelated to local publish/invoke are no-ops.
/// </summary>
public sealed class TestMessageBus : IMessageBus
{
    public List<object> PublishedEvents { get; } = [];

    public string? TenantId { get; set; }

    public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
    {
        if (message is not null)
        {
            PublishedEvents.Add(message);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
    {
        if (message is not null)
        {
            PublishedEvents.Add(message);
        }
        return ValueTask.CompletedTask;
    }

    public Task InvokeAsync(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        PublishedEvents.Add(message);
        return Task.CompletedTask;
    }

    public Task InvokeAsync(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        PublishedEvents.Add(message);
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public Task<T> InvokeAsync<T>(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public Task InvokeForTenantAsync(
        string tenantId,
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        PublishedEvents.Add(message);
        return Task.CompletedTask;
    }

    public Task<T> InvokeForTenantAsync<T>(
        string tenantId,
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public IDestinationEndpoint EndpointFor(string endpointName) =>
        throw new NotImplementedException();

    public IDestinationEndpoint EndpointFor(Uri uri) => throw new NotImplementedException();

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message) => [];

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options) =>
        [];

    public ValueTask BroadcastToTopicAsync(
        string topicName,
        object message,
        DeliveryOptions? options = null
    )
    {
        PublishedEvents.Add(message);
        return ValueTask.CompletedTask;
    }
}
