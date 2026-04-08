namespace SimpleModule.BackgroundJobs.Contracts;

public enum JobQueueEntryState
{
    Pending = 0,
    Claimed = 1,
    Completed = 2,
    Failed = 3,
}
