using System.Text.Json;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.Services;

internal sealed class DefaultJobExecutionContext(
    JobId jobId,
    string? serializedData,
    ProgressChannel channel
) : IJobExecutionContext
{
    public JobId JobId => jobId;

    public T GetData<T>()
    {
        if (string.IsNullOrEmpty(serializedData))
        {
            throw new InvalidOperationException("No data was provided for this job.");
        }

        return JsonSerializer.Deserialize<T>(serializedData)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize job data as {typeof(T).Name}."
            );
    }

    public void ReportProgress(int percentage, string? message = null)
    {
        channel.Enqueue(new ProgressEntry(jobId.Value, percentage, message, LogMessage: null, DateTimeOffset.UtcNow));
    }

    public void Log(string message)
    {
        channel.Enqueue(new ProgressEntry(jobId.Value, Percentage: -1, Message: null, message, DateTimeOffset.UtcNow));
    }
}
