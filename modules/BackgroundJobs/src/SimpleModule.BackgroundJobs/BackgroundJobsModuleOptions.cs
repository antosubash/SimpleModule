using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs;

public enum BackgroundJobsWorkerMode
{
    Producer = 0,
    Consumer = 1,
}

public class BackgroundJobsModuleOptions : IModuleOptions
{
    public BackgroundJobsWorkerMode WorkerMode { get; set; } = BackgroundJobsWorkerMode.Producer;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public int ProgressFlushBatchSize { get; set; } = 50;
    public TimeSpan ProgressFlushInterval { get; set; } = TimeSpan.FromSeconds(2);
    public int MaxLogEntries { get; set; } = 1000;
}
