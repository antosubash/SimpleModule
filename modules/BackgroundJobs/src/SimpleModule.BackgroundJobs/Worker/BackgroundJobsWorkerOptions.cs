// modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Worker/BackgroundJobsWorkerOptions.cs
namespace SimpleModule.BackgroundJobs.Worker;

public sealed class BackgroundJobsWorkerOptions
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan StallTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan StallSweepInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(10);
}
