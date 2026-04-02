using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace SimpleModule.BackgroundJobs;

[Module(
    BackgroundJobsConstants.ModuleName,
    RoutePrefix = BackgroundJobsConstants.RoutePrefix,
    ViewPrefix = "/admin/jobs"
)]
public class BackgroundJobsModule : IModule
{
    private bool _tickerQRegistered;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<BackgroundJobsDbContext>(
            configuration,
            BackgroundJobsConstants.ModuleName
        );

        services.AddSingleton(sp =>
        {
            var registry = new JobTypeRegistry();
            foreach (var reg in sp.GetServices<ModuleJobRegistration>())
            {
                registry.Register(reg.JobType);
            }
            return registry;
        });

        var isTesting = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"],
            "Testing",
            StringComparison.OrdinalIgnoreCase
        );

        if (!isTesting)
        {
            _tickerQRegistered = true;

            services.AddTickerQ(options =>
            {
                options.SetExceptionHandler<JobExceptionHandler>();
                options.AddOperationalStore<TimeTickerEntity, CronTickerEntity>(ef =>
                    ef.UseApplicationDbContext<BackgroundJobsDbContext>(
                        ConfigurationType.UseModelCustomizer
                    )
                );
            });

            services.AddSingleton<ProgressChannel>();
            services.AddHostedService<ProgressFlushService>();
        }
        else
        {
            services.AddSingleton<ProgressChannel>();
            services.AddSingleton(typeof(ITimeTickerManager<TimeTickerEntity>), _ =>
                NoOpTickerManagerFactory.CreateTimeManager());
            services.AddSingleton(typeof(ICronTickerManager<CronTickerEntity>), _ =>
                NoOpTickerManagerFactory.CreateCronManager());
        }

        services.AddScoped<IBackgroundJobs, BackgroundJobsService>();
        services.AddScoped<IBackgroundJobsContracts, BackgroundJobsContractsService>();
    }

    public void ConfigureHost(IHost host)
    {
        if (!_tickerQRegistered)
        {
            return;
        }

        // Ensure tables exist before TickerQ's hosted services start
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BackgroundJobsDbContext>();
        if (!db.Database.EnsureCreated())
        {
            try
            {
                db.GetService<IRelationalDatabaseCreator>()?.CreateTables();
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                // Tables already exist
            }
        }

        host.UseTickerQ();
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<BackgroundJobsPermissions>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Background Jobs",
                Url = "/admin/jobs",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M5.636 18.364a9 9 0 010-12.728m12.728 0a9 9 0 010 12.728M12 12v.01M8.464 15.536a5 5 0 010-7.072m7.072 0a5 5 0 010 7.072"/></svg>""",
                Order = 95,
                Section = MenuSection.AdminSidebar,
                Group = "Background Jobs",
            }
        );
    }
}
