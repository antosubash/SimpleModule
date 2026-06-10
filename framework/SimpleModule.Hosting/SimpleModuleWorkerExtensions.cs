using System.Reflection;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Database.Interceptors;
using Wolverine;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.Hosting;

public static class SimpleModuleWorkerExtensions
{
    /// <summary>
    /// Configures a Generic Host as a SimpleModule worker:
    /// registers all modules (via the source-generated <c>AddModules</c>),
    /// forces BackgroundJobs into Consumer mode, wires the event bus and
    /// EF interceptors, but skips all ASP.NET-specific middleware and endpoints.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="moduleAssemblies">
    /// Module assemblies to scan for Wolverine handlers. Pass
    /// <c>SimpleModule.Core.ModuleExtensions.ModuleAssemblies</c> from your worker's
    /// <c>Program.cs</c>. If empty, only the entry assembly is scanned, so handlers
    /// living in other module assemblies will not be discovered.
    /// </param>
    public static HostApplicationBuilder AddSimpleModuleWorker(
        this HostApplicationBuilder builder,
        params Assembly[] moduleAssemblies
    )
    {
        SimpleModuleHostExtensions.BridgeAspireConnectionString(builder.Configuration);

        // Force consumer mode regardless of config. User can still tune Worker:* options.
        builder.Configuration["BackgroundJobs:WorkerMode"] = "Consumer";

        builder
            .Services.AddFusionCache()
            .WithDefaultEntryOptions(o => o.Duration = TimeSpan.FromMinutes(5));

        var workerProvider = SimpleModuleHostExtensions.ValidateDatabaseConfiguration(
            builder.Configuration
        );
        var dbConnectionString = builder.Configuration["Database:DefaultConnection"]!;

        builder.UseWolverine(opts =>
            WolverineConfiguration.Configure(
                opts,
                moduleAssemblies,
                workerProvider,
                dbConnectionString
            )
        );

        builder.Services.AddResourceSetupOnStartup();
        // Lazy<IMessageBus> breaks the SettingsService ↔ AuditingMessageBus ↔
        // ISettingsContracts construction cycle.
        builder.Services.AddScoped(sp => new Lazy<IMessageBus>(() =>
            sp.GetRequiredService<IMessageBus>()
        ));

        // HttpContextAccessor: EntityInterceptor returns null in non-HTTP contexts.
        builder.Services.AddHttpContextAccessor();

        // AddModules() registers IPolicy<T> implementations, so the dispatcher must
        // resolve here too — background handlers may authorize resources as well.
        builder.Services.AddOptions<PolicyAuthorizationOptions>();
        builder.Services.AddScoped<IAuthorizer, Authorizer>();

        builder.Services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, EntityChangeInterceptor>();

        return builder;
    }
}
