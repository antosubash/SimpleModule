using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Events;
using Wolverine;
using Wolverine.Tracking;

namespace SimpleModule.Database.Tests;

/// <summary>
/// Locks in the contract the host's <c>UseWolverine(...)</c> wiring relies on: handlers
/// in any module assembly registered via <c>options.Discovery.IncludeAssembly</c> are
/// discovered and invoked. If Wolverine ever changes this discovery contract, the
/// auto-include flow in <c>SimpleModuleHostExtensions</c> / <c>SimpleModuleWorkerExtensions</c>
/// would silently stop picking up handlers — this test catches that.
/// </summary>
public sealed class WolverineAssemblyDiscoveryTests
{
    [Fact]
    public async Task Handler_In_Included_Assembly_Is_Invoked()
    {
        DiscoveryTestHandler.Reset();

        var builder = Host.CreateApplicationBuilder();
        builder.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(WolverineAssemblyDiscoveryTests).Assembly);
        });

        using var host = builder.Build();
        await host.StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();

        // Wait for the handler to actually execute before asserting — PublishAsync
        // is fire-and-forget; on slower runners (Linux CI) the assertion otherwise
        // races against the in-memory dispatcher.
        await host.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .ExecuteAndWaitAsync(
                (Func<IMessageContext, Task>)(
                    async _ =>
                    {
                        await bus.PublishAsync(new DiscoveryTestEvent("hello"));
                    }
                )
            );

        DiscoveryTestHandler.LastPayload.Should().Be("hello");

        await host.StopAsync();
    }
}

public sealed record DiscoveryTestEvent(string Payload) : DomainEvent;

public static class DiscoveryTestHandler
{
    public static string? LastPayload { get; private set; }

    public static void Reset() => LastPayload = null;

    public static void Handle(DiscoveryTestEvent @event) => LastPayload = @event.Payload;
}
