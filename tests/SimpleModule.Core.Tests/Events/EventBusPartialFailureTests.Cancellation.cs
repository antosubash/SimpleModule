using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.Core.Events;

namespace SimpleModule.Core.Tests.Events;

public sealed partial class EventBusPartialFailureTests
{
    /// <summary>
    /// Test: CancellationToken is propagated to all handlers.
    /// This ensures handlers can respond to cancellation requests.
    /// </summary>
    [Fact]
    public async Task PublishAsync_CancellationToken_IsPropagatedToAllHandlers()
    {
        var receivedTokens = new List<CancellationToken>();

        var handler1 = new DelegateHandler(
            (_, ct) =>
            {
                receivedTokens.Add(ct);
                return Task.CompletedTask;
            }
        );
        var handler2 = new DelegateHandler(
            (_, ct) =>
            {
                receivedTokens.Add(ct);
                return Task.CompletedTask;
            }
        );

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

        using var cts = new CancellationTokenSource();
        await bus.PublishAsync(new TestEvent("test"), cts.Token);

        receivedTokens.Should().HaveCount(2);
        receivedTokens.Should().AllSatisfy(t => t.Should().Be(cts.Token));
    }

    /// <summary>
    /// Test: CancellationToken is propagated even when handlers fail.
    /// </summary>
    [Fact]
    public async Task PublishAsync_CancellationToken_PropagatedEvenWhenHandlersFail()
    {
        var receivedToken = default(CancellationToken);
        var handler = new DelegateHandler(
            (_, ct) =>
            {
                receivedToken = ct;
                return Task.CompletedTask;
            }
        );

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        services.AddSingleton<IEventHandler<TestEvent>>(new FailingHandler());
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

        using var cts = new CancellationTokenSource();

        var act = async () => await bus.PublishAsync(new TestEvent("test"), cts.Token);
        await act.Should().ThrowAsync<AggregateException>();

        receivedToken.Should().Be(cts.Token);
    }

    /// <summary>
    /// Test: When no handlers fail, no exception is thrown.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenAllHandlersSucceed_DoesNotThrow()
    {
        var handler1 = new SuccessfulHandler();
        var handler2 = new SuccessfulHandler();

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        await act.Should().NotThrowAsync();
        handler1.Executed.Should().BeTrue();
        handler2.Executed.Should().BeTrue();
    }

    /// <summary>
    /// Test: When no handlers are registered, no exception is thrown.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenNoHandlersRegistered_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var bus = new EventBus(
            provider,
            NullLogger<EventBus>.Instance,
            new BackgroundEventChannel(NullLogger<BackgroundEventChannel>.Instance)
        );

        var act = async () => await bus.PublishAsync(new TestEvent("test"));

        await act.Should().NotThrowAsync();
    }
}
