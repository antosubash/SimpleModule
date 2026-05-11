using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace SimpleModule.Tests.Shared.Fakes;

/// <summary>
/// Minimal in-memory <see cref="IDbContextOutbox{T}"/> for unit-testing services that
/// were migrated to the transactional outbox pattern. Records every PublishAsync /
/// SendAsync call for assertion, and flushes by calling SaveChangesAsync on the
/// wrapped DbContext so DB state matches what the production outbox would commit.
/// Outbox envelope persistence is intentionally not simulated — that is verified
/// by integration tests against a real Wolverine host.
/// </summary>
public sealed class FakeDbContextOutbox<TDbContext>(TDbContext context)
    : IDbContextOutbox<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext DbContext { get; } = context;

    public List<object> PublishedMessages { get; } = [];
    public List<object> SentMessages { get; } = [];

    public string? TenantId { get; set; }

    public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
    {
        if (message is not null)
        {
            PublishedMessages.Add(message);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
    {
        if (message is not null)
        {
            SentMessages.Add(message);
        }
        return ValueTask.CompletedTask;
    }

    public async Task SaveChangesAndFlushMessagesAsync(CancellationToken token = default)
    {
        await DbContext.SaveChangesAsync(token);
    }

    public Task FlushOutgoingMessagesAsync() => Task.CompletedTask;

    // The remaining IMessageBus surface is not used by service unit tests today;
    // throwing makes accidental use loud.
    public Task InvokeAsync(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

    public Task InvokeAsync(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    ) => throw new NotImplementedException();

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
    ) => throw new NotImplementedException();

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
    ) => throw new NotImplementedException();
}
