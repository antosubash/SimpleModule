namespace SimpleModule.BackgroundJobs.Contracts;

/// <summary>
/// Fluent builder returned by <see cref="IScheduler.Job{TJob}(string?)"/>. All
/// methods mutate the underlying <see cref="ScheduledJobDefinition"/> and return
/// the same builder so calls can chain.
/// </summary>
public interface IScheduledJob<TJob>
    where TJob : IModuleJob
{
    IScheduledJob<TJob> Cron(string expression);

    IScheduledJob<TJob> EveryMinutes(int minutes);

    IScheduledJob<TJob> Hourly();

    IScheduledJob<TJob> Daily();

    /// <summary>Run once a day at <paramref name="time"/> formatted as <c>HH:mm</c>.</summary>
    IScheduledJob<TJob> DailyAt(string time);

    /// <summary>Run at midnight Monday-Friday in the configured timezone.</summary>
    IScheduledJob<TJob> Weekdays();

    /// <summary>IANA timezone id (e.g. <c>UTC</c>, <c>America/New_York</c>).</summary>
    IScheduledJob<TJob> Timezone(string tz);

    /// <summary>Skip the run if a previous invocation of the same job is still in-flight.</summary>
    IScheduledJob<TJob> WithoutOverlapping();

    /// <summary>Run on a single host even when many hosts share the schedule.</summary>
    IScheduledJob<TJob> OnOneServer();

    /// <summary>Attach a payload object that is serialised into <see cref="IJobExecutionContext"/>.</summary>
    IScheduledJob<TJob> WithPayload(object payload);
}
