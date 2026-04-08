// modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Worker/WorkerIdentity.cs
namespace SimpleModule.BackgroundJobs.Worker;

public sealed record WorkerIdentity(string Id)
{
    public static WorkerIdentity Create()
    {
        var raw = $"{Environment.MachineName}-{Environment.ProcessId}-{Guid.NewGuid():N}";
        return new WorkerIdentity(raw.Length <= 100 ? raw : raw[..100]);
    }
}
