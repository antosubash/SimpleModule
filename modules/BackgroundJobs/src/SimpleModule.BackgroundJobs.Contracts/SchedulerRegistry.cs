using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs.Contracts;

/// <summary>
/// Singleton implementation of <see cref="IScheduler"/>. Modules invoke
/// <c>services.AddScheduledJobs(scheduler =&gt; scheduler.Job&lt;T&gt;()...)</c> during
/// <c>ConfigureServices</c>; the registry accumulates definitions which the
/// hosted <c>SchedulerService</c> reconciles against the database on each tick.
/// </summary>
[NoDtoGeneration]
public sealed class SchedulerRegistry : IScheduler
{
    private readonly List<ScheduledJobDefinition> _definitions = [];
    private readonly Lock _lock = new();

    public IScheduledJob<TJob> Job<TJob>(string? name = null)
        where TJob : IModuleJob
    {
        var jobType = typeof(TJob);
        var resolvedName = string.IsNullOrWhiteSpace(name) ? jobType.FullName! : name;

        lock (_lock)
        {
            var existing = _definitions.FirstOrDefault(d =>
                string.Equals(d.Name, resolvedName, StringComparison.Ordinal)
            );
            if (existing is not null)
            {
                throw new InvalidOperationException(
                    $"A scheduled job named '{resolvedName}' is already registered."
                );
            }

            var definition = new ScheduledJobDefinition { Name = resolvedName, JobType = jobType };
            _definitions.Add(definition);
            return new ScheduledJobBuilder<TJob>(definition);
        }
    }

    public IReadOnlyList<ScheduledJobDefinition> Definitions
    {
        get
        {
            lock (_lock)
            {
                return [.. _definitions];
            }
        }
    }
}
