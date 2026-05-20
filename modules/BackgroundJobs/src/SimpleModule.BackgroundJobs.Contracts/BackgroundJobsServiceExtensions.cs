using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleModule.BackgroundJobs.Contracts;

public static class BackgroundJobsServiceExtensions
{
    public static IServiceCollection AddModuleJob<TJob>(this IServiceCollection services)
        where TJob : class, IModuleJob
    {
        services.AddScoped<TJob>();
        services.AddSingleton(new ModuleJobRegistration(typeof(TJob)));
        return services;
    }

    /// <summary>
    /// Register declarative scheduled jobs. Safe to call from multiple modules:
    /// the first call creates the shared <see cref="SchedulerRegistry"/>; subsequent
    /// calls reuse the same instance.
    /// </summary>
    public static IServiceCollection AddScheduledJobs(
        this IServiceCollection services,
        Action<IScheduler> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);

        var descriptor = services.FirstOrDefault(s =>
            s.ServiceType == typeof(IScheduler) && s.ImplementationInstance is SchedulerRegistry
        );

        SchedulerRegistry registry;
        if (descriptor?.ImplementationInstance is SchedulerRegistry existing)
        {
            registry = existing;
        }
        else
        {
            registry = new SchedulerRegistry();
            services.RemoveAll<IScheduler>();
            services.AddSingleton<IScheduler>(registry);
        }

        configure(registry);
        return services;
    }
}

public class ModuleJobRegistration
{
    public ModuleJobRegistration(Type jobType)
    {
        JobType = jobType;
    }

    public ModuleJobRegistration() { }

    public Type JobType { get; set; } = null!;
}
