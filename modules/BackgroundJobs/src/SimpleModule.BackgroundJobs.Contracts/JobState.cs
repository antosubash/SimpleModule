namespace SimpleModule.BackgroundJobs.Contracts;

public enum JobState
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Skipped,
}
