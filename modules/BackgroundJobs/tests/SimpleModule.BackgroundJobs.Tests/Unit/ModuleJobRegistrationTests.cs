using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.BackgroundJobs.Contracts;

namespace BackgroundJobs.Tests.Unit;

public sealed class ModuleJobRegistrationTests
{
    [Fact]
    public void AddModuleJob_RegistersJobInDI()
    {
        var services = new ServiceCollection();

        services.AddModuleJob<TestJob>();

        var provider = services.BuildServiceProvider();
        var job = provider.GetService<TestJob>();
        job.Should().NotBeNull();
    }

    [Fact]
    public void AddModuleJob_RegistersModuleJobRegistration()
    {
        var services = new ServiceCollection();

        services.AddModuleJob<TestJob>();

        var provider = services.BuildServiceProvider();
        var registrations = provider.GetServices<ModuleJobRegistration>().ToList();
        registrations.Should().HaveCount(1);
        registrations[0].JobType.Should().Be(typeof(TestJob));
    }

    [Fact]
    public void AddModuleJob_MultipleJobs_AllRegistered()
    {
        var services = new ServiceCollection();

        services.AddModuleJob<TestJob>();
        services.AddModuleJob<AnotherJob>();

        var provider = services.BuildServiceProvider();
        var registrations = provider.GetServices<ModuleJobRegistration>().ToList();
        registrations.Should().HaveCount(2);
    }

    private sealed class TestJob : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct) =>
            Task.CompletedTask;
    }

    private sealed class AnotherJob : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct) =>
            Task.CompletedTask;
    }
}
