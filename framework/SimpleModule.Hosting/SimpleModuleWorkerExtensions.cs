using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Events;
using SimpleModule.Database.Interceptors;

namespace SimpleModule.Hosting;

public static class SimpleModuleWorkerExtensions
{
    /// <summary>
    /// Configures a Generic Host as a SimpleModule worker:
    /// registers all modules (via the source-generated <c>AddModules</c>),
    /// forces BackgroundJobs into Consumer mode, wires the event bus and
    /// EF interceptors, but skips all ASP.NET-specific middleware and endpoints.
    /// </summary>
    public static HostApplicationBuilder AddSimpleModuleWorker(this HostApplicationBuilder builder)
    {
        // Force consumer mode regardless of config. User can still tune Worker:* options.
        builder.Configuration["BackgroundJobs:WorkerMode"] = "Consumer";

        // Core infrastructure that the worker needs:
        builder.Services.AddSingleton<BackgroundEventChannel>();
        builder.Services.AddHostedService<BackgroundEventDispatcher>();
        builder.Services.AddScoped<IEventBus, EventBus>();

        // EF interceptors (entities expect these when SaveChanges is called):
        builder.Services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DomainEventInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, EntityChangeInterceptor>();

        return builder;
    }
}
