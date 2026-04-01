using Microsoft.Extensions.DependencyInjection;

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
}

public record ModuleJobRegistration(Type JobType);
