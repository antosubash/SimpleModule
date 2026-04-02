using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;

namespace SimpleModule.BackgroundJobs.Services;

public sealed partial class JobExceptionHandler(ILogger<JobExceptionHandler> logger)
    : ITickerExceptionHandler
{
    public Task HandleExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        LogJobFailed(logger, tickerId, tickerType, exception);
        return Task.CompletedTask;
    }

    public Task HandleCanceledExceptionAsync(
        Exception exception,
        Guid tickerId,
        TickerType tickerType
    )
    {
        LogJobCancelled(logger, tickerId, exception);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Background job {TickerId} ({TickerType}) failed"
    )]
    private static partial void LogJobFailed(
        ILogger logger,
        Guid tickerId,
        TickerType tickerType,
        Exception ex
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "Background job {TickerId} was cancelled")]
    private static partial void LogJobCancelled(ILogger logger, Guid tickerId, Exception ex);
}
