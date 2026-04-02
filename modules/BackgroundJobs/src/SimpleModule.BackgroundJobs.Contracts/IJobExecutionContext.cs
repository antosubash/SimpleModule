namespace SimpleModule.BackgroundJobs.Contracts;

public interface IJobExecutionContext
{
    JobId JobId { get; }
    T GetData<T>();
    void ReportProgress(int percentage, string? message = null);
    void Log(string message);
}
