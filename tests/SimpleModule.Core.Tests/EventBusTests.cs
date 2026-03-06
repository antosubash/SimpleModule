using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Events;

namespace SimpleModule.Core.Tests;

public sealed class EventBusTests
{
    private sealed record TestEvent(string Value) : IEvent;

    private sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = [];

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            ReceivedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PublishAsync_WithRegisteredHandler_InvokesHandler()
    {
        var handler = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider);

        await bus.PublishAsync(new TestEvent("test"));

        handler.ReceivedEvents.Should().ContainSingle().Which.Value.Should().Be("test");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_InvokesAll()
    {
        var handler1 = new TestEventHandler();
        var handler2 = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider);

        await bus.PublishAsync(new TestEvent("test"));

        handler1.ReceivedEvents.Should().ContainSingle();
        handler2.ReceivedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task PublishAsync_WithNoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(provider);

        var act = () => bus.PublishAsync(new TestEvent("test"));

        await act.Should().NotThrowAsync();
    }
}
