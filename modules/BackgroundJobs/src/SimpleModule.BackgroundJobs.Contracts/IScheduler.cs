namespace SimpleModule.BackgroundJobs.Contracts;

/// <summary>
/// Declarative scheduling surface for recurring jobs. Modules call this from
/// <c>IModule.ConfigureServices</c> to register schedules at startup time;
/// the hosted <c>SchedulerService</c> turns due definitions into queued jobs.
/// </summary>
public interface IScheduler
{
    IScheduledJob<TJob> Job<TJob>(string? name = null)
        where TJob : IModuleJob;

    IReadOnlyList<ScheduledJobDefinition> Definitions { get; }
}
