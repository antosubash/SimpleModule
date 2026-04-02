using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.BackgroundJobs.Services;
using TickerQ.Utilities.Enums;

namespace BackgroundJobs.Tests.Unit;

public sealed class JobExceptionHandlerTests
{
    private readonly JobExceptionHandler _sut = new(NullLogger<JobExceptionHandler>.Instance);

    [Fact]
    public async Task HandleExceptionAsync_DoesNotThrow()
    {
        var exception = new InvalidOperationException("Test error");

        var act = () => _sut.HandleExceptionAsync(exception, Guid.NewGuid(), TickerType.TimeTicker);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleCanceledExceptionAsync_DoesNotThrow()
    {
        var exception = new OperationCanceledException("Cancelled");

        var act = () =>
            _sut.HandleCanceledExceptionAsync(exception, Guid.NewGuid(), TickerType.TimeTicker);

        await act.Should().NotThrowAsync();
    }
}
