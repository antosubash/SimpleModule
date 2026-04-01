namespace SimpleModule.BackgroundJobs.Contracts;

public class JobFilter
{
    public JobState? State { get; set; }
    public string? JobType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
