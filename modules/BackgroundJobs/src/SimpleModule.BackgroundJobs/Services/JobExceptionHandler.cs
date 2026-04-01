using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Interfaces;

namespace SimpleModule.BackgroundJobs.Services;

public sealed partial class JobExceptionHandler(ILogger<JobExceptionHandler> logger)
    : ITickerExceptionHandler
{
    public Task HandleExceptionAsync(Exception exception)
    {
        LogJobFailed(logger, exception);
        return Task.CompletedTask;
    }

    public Task HandleCanceledExceptionAsync(OperationCanceledException exception)
    {
        LogJobCancelled(logger, exception);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Background job failed")]
    private static partial void LogJobFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Background job was cancelled")]
    private static partial void LogJobCancelled(ILogger logger, Exception ex);
}
