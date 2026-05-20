using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.Tests.Scheduler;

public sealed class FakeJobA : IModuleJob
{
    public Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class FakeJobB : IModuleJob
{
    public Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class FakeJobC : IModuleJob
{
    public Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
