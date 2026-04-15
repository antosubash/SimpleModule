using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using Wolverine;

namespace SimpleModule.AuditLogs.Pipeline;

/// <summary>
/// Decorator over Wolverine's <see cref="IMessageBus"/> that captures an audit
/// entry every time an <see cref="IEvent"/> is published or invoked. Audit
/// failures are logged but never propagate — auditing must not break primary
/// operations.
/// </summary>
public sealed class AuditingMessageBus(
    IMessageBus inner,
    IAuditContext auditContext,
    AuditChannel channel,
    ISettingsContracts? settings = null,
    ILogger<AuditingMessageBus>? logger = null
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
        if (message is IEvent evt)
        {
            await AuditAsync(evt);
        }
    }

    public async ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
    {
        await inner.SendAsync(message, options);
        if (message is IEvent evt)
        {
            await AuditAsync(evt);
        }
    }

    public async Task InvokeAsync(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        await inner.InvokeAsync(message, cancellation, timeout);
        if (message is IEvent evt)
        {
            await AuditAsync(evt);
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
        if (message is IEvent evt)
        {
            await AuditAsync(evt);
        }
    }

    public async Task<T> InvokeAsync<T>(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = default
    )
    {
        var result = await inner.InvokeAsync<T>(message, cancellation, timeout);
        if (message is IEvent evt)
        {
            await AuditAsync(evt);
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
        if (message is IEvent evt)
        {
            await AuditAsync(evt);
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

    private async Task AuditAsync(IEvent evt)
    {
        var enabled =
            settings is null
            || await settings.GetSettingAsync<bool>("auditlogs.capture.domain", SettingScope.System)
                != false;

        if (!enabled)
        {
            return;
        }

        try
        {
            var entry = AuditEntryExtractor.Extract(evt, auditContext);
            channel.Enqueue(entry);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            // Audit failures must never break primary operations.
            logger?.LogError(ex, "Failed to enqueue audit entry; audit will not be recorded");
        }
#pragma warning restore CA1031
    }
}
