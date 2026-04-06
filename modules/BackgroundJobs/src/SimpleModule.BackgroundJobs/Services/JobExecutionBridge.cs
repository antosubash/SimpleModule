using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using TickerQ.Utilities.Base;

namespace SimpleModule.BackgroundJobs.Services;

public sealed partial class JobExecutionBridge(
    IServiceProvider serviceProvider,
    JobTypeRegistry registry,
    ProgressChannel channel,
    ILogger<JobExecutionBridge> logger
)
{
    [TickerFunction(BackgroundJobsInternalConstants.DispatcherFunctionName)]
    public async Task ExecuteAsync(
        TickerFunctionContext<JobDispatchPayload> context,
        CancellationToken ct
    )
    {
        var jobType =
            registry.Resolve(context.Request.JobTypeName)
            ?? throw new InvalidOperationException(
                $"Unregistered job type: {context.Request.JobTypeName}"
            );

        LogJobStarting(logger, jobType.Name, context.Id);

        await using var scope = serviceProvider.CreateAsyncScope();
        var job = (IModuleJob)scope.ServiceProvider.GetRequiredService(jobType);
        var executionContext = new DefaultJobExecutionContext(
            JobId.From(context.Id),
            context.Request,
            channel
        );

        executionContext.ReportProgress(0, "Starting");
        await job.ExecuteAsync(executionContext, ct);
        executionContext.ReportProgress(100, "Completed");

        LogJobCompleted(logger, jobType.Name, context.Id);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {JobType} ({JobId}) starting")]
    private static partial void LogJobStarting(ILogger logger, string jobType, Guid jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {JobType} ({JobId}) completed")]
    private static partial void LogJobCompleted(ILogger logger, string jobType, Guid jobId);
}
