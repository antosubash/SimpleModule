using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Queue;
using SimpleModule.BackgroundJobs.Services;
using SimpleModule.BackgroundJobs.Worker;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Database;

namespace SimpleModule.BackgroundJobs;

[Module(
    BackgroundJobsConstants.ModuleName,
    RoutePrefix = BackgroundJobsConstants.RoutePrefix,
    ViewPrefix = BackgroundJobsConstants.ViewPrefix
)]
public class BackgroundJobsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<BackgroundJobsDbContext>(configuration, BackgroundJobsConstants.ModuleName);

        var section = configuration.GetSection("BackgroundJobs");
        services.Configure<BackgroundJobsModuleOptions>(section);
        services.Configure<BackgroundJobsWorkerOptions>(configuration.GetSection("BackgroundJobs:Worker"));

        var opts = section.Get<BackgroundJobsModuleOptions>() ?? new BackgroundJobsModuleOptions();

        services.AddSingleton(sp =>
        {
            var registry = new JobTypeRegistry();
            foreach (var reg in sp.GetServices<ModuleJobRegistration>())
            {
                registry.Register(reg.JobType);
            }
            return registry;
        });

        services.AddSingleton<ProgressChannel>();
        services.AddScoped<IJobQueue, DatabaseJobQueue>();
        services.AddScoped<IBackgroundJobs, BackgroundJobsService>();
        services.AddScoped<IBackgroundJobsContracts, BackgroundJobsContractsService>();

        // Progress flushing runs in whichever host owns the module — both producer and consumer.
        services.AddHostedService<ProgressFlushService>();

        if (opts.WorkerMode == BackgroundJobsWorkerMode.Consumer)
        {
            services.AddSingleton(WorkerIdentity.Create());
            services.AddHostedService<JobProcessorService>();
            services.AddHostedService<StalledJobSweeperService>();
        }
    }

    public void ConfigureHost(IHost host)
    {
        // Ensure schema exists on first run (dev convenience; prod uses migrations).
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BackgroundJobsDbContext>();
        if (!db.Database.EnsureCreated())
        {
            try { db.GetService<IRelationalDatabaseCreator>()?.CreateTables(); }
#pragma warning disable CA1031
            catch { /* tables already exist */ }
#pragma warning restore CA1031
        }
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<BackgroundJobsPermissions>();
    }

    // Menu items removed — accessible via Admin hub page
}
