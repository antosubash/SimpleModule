using FluentAssertions;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed class JobTypeRegistryTests
{
    private readonly JobTypeRegistry _sut = new();

    [Fact]
    public void Register_AddsType_CanResolve()
    {
        _sut.Register(typeof(TestJob));

        var resolved = _sut.Resolve(typeof(TestJob).AssemblyQualifiedName!);

        resolved.Should().Be(typeof(TestJob));
    }

    [Fact]
    public void Resolve_UnregisteredType_ReturnsNull()
    {
        var resolved = _sut.Resolve("SomeUnknownType, SomeAssembly");

        resolved.Should().BeNull();
    }

    [Fact]
    public void IsRegistered_RegisteredType_ReturnsTrue()
    {
        _sut.Register(typeof(TestJob));

        _sut.IsRegistered(typeof(TestJob).AssemblyQualifiedName!).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_UnregisteredType_ReturnsFalse()
    {
        _sut.IsRegistered("Unknown").Should().BeFalse();
    }

    [Fact]
    public void All_ReturnsAllRegisteredTypes()
    {
        _sut.Register(typeof(TestJob));
        _sut.Register(typeof(AnotherTestJob));

        _sut.All.Should().HaveCount(2);
        _sut.All.Values.Should().Contain(typeof(TestJob));
        _sut.All.Values.Should().Contain(typeof(AnotherTestJob));
    }

    [Fact]
    public void Register_SameTypeTwice_Overwrites()
    {
        _sut.Register(typeof(TestJob));
        _sut.Register(typeof(TestJob));

        _sut.All.Should().HaveCount(1);
    }

    private sealed class TestJob : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class AnotherTestJob : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
