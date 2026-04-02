using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs;

public class BackgroundJobsModuleOptions : IModuleOptions
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;

    public int ProgressFlushBatchSize { get; set; } = 50;

    public TimeSpan ProgressFlushInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int MaxLogEntries { get; set; } = 1000;
}
