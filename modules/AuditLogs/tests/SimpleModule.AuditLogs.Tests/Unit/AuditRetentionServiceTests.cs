using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.AuditLogs;
using SimpleModule.AuditLogs.Retention;

namespace AuditLogs.Tests.Unit;

public sealed class AuditRetentionServiceTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().BuildServiceProvider();

    public void Dispose() => _provider.Dispose();

    /// <summary>
    /// A host that aborts startup (a failed seed, a bad connection string) cancels
    /// background services while this one is still inside its one-minute startup
    /// delay. If that cancellation escapes <c>ExecuteAsync</c>, .NET treats it as a
    /// crashed BackgroundService and — under the default
    /// <c>BackgroundServiceExceptionBehavior.StopHost</c> — logs a critical
    /// "BackgroundService failed" that buries the error that actually stopped the host.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenStoppedDuringStartupDelay_DoesNotSurfaceCancellation()
    {
        using var service = CreateService();
        using var stopping = new CancellationTokenSource();

        await service.StartAsync(stopping.Token);
        service.ExecuteTask.Should().NotBeNull();
        var executeTask = service.ExecuteTask!;

        // Cancel the token StartAsync linked against, then await the execute task
        // directly rather than going through StopAsync. Awaiting is deterministic —
        // it resolves only once the task reaches a terminal state, completing when
        // the cancellation is handled and throwing TaskCanceledException when it
        // escapes. Inspecting Status after StopAsync instead races the state machine.
        await stopping.CancelAsync();

        var awaitExecuteTask = async () => await executeTask;

        await awaitExecuteTask
            .Should()
            .NotThrowAsync(
                "a cancelled startup delay is a normal shutdown, not a BackgroundService crash"
            );
    }

    private AuditRetentionService CreateService() =>
        new(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AuditLogsModuleOptions()),
            NullLogger<AuditRetentionService>.Instance
        );
}
