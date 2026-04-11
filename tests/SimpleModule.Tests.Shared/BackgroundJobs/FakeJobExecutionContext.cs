using System.Text.Json;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.Tests.Shared.BackgroundJobs;

/// <summary>
/// Test fake for <see cref="IJobExecutionContext"/>. Captures progress reports
/// and log messages in-memory so tests can assert on what a job did without
/// wiring up a <c>ProgressChannel</c> or the real <c>DefaultJobExecutionContext</c>.
/// </summary>
public sealed class FakeJobExecutionContext : IJobExecutionContext
{
    private readonly string? _serializedData;
    private readonly List<ProgressReport> _progressReports = [];
    private readonly List<string> _logMessages = [];

    public FakeJobExecutionContext(JobId jobId, string? serializedData = null)
    {
        JobId = jobId;
        _serializedData = serializedData;
    }

    public FakeJobExecutionContext(JobId jobId, object data)
        : this(jobId, JsonSerializer.Serialize(data)) { }

    public FakeJobExecutionContext()
        : this(JobId.From(Guid.NewGuid()), serializedData: null) { }

    public JobId JobId { get; }

    public IReadOnlyList<ProgressReport> ProgressReports => _progressReports;

    public IReadOnlyList<string> LogMessages => _logMessages;

    public T GetData<T>()
    {
        if (string.IsNullOrEmpty(_serializedData))
        {
            throw new InvalidOperationException("No data was provided for this job.");
        }

        return JsonSerializer.Deserialize<T>(_serializedData)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize job data as {typeof(T).Name}."
            );
    }

    public void ReportProgress(int percentage, string? message = null) =>
        _progressReports.Add(new ProgressReport(percentage, message));

    public void Log(string message) => _logMessages.Add(message);

    public readonly record struct ProgressReport(int Percentage, string? Message);
}
