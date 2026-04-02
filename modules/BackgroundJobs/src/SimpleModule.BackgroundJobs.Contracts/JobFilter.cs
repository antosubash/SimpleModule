namespace SimpleModule.BackgroundJobs.Contracts;

public class JobFilter
{
    public JobState? State { get; set; }
    public string? JobType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public static JobFilter FromQuery(
        JobState? state = null,
        string? jobType = null,
        int? page = null,
        int? pageSize = null
    ) =>
        new()
        {
            State = state,
            JobType = jobType,
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
        };
}
