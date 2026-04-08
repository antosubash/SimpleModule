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
using SimpleModule.Core.Menu;
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

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(new MenuItem
        {
            Label = "Background Jobs",
            Url = BackgroundJobsConstants.ViewPrefix,
            Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M5.636 18.364a9 9 0 010-12.728m12.728 0a9 9 0 010 12.728M12 12v.01M8.464 15.536a5 5 0 010-7.072m7.072 0a5 5 0 010 7.072"/></svg>""",
            Order = 95,
            Section = MenuSection.AdminSidebar,
            Group = "Background Jobs",
        });
    }
}
