using SimpleModule.Core.Events;

namespace SimpleModule.Tests.Shared.Fakes;

/// <summary>
/// Recording <see cref="IEventBus"/> stub for unit tests. Captures every published
/// event in <see cref="PublishedEvents"/> so tests can assert on publishing behaviour
/// without wiring up the full event pipeline.
/// </summary>
public sealed class TestEventBus : IEventBus
{
    public List<IEvent> PublishedEvents { get; } = [];

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        PublishedEvents.Add(@event);
        return Task.CompletedTask;
    }

    public void PublishInBackground<T>(T @event)
        where T : IEvent => PublishedEvents.Add(@event);
}
