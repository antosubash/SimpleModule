using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scrutor;
using SimpleModule.Core.Broadcasting;
using Wolverine;

namespace SimpleModule.Hosting.Broadcasting;

public static class BroadcastingExtensions
{
    /// <summary>
    /// Registers SignalR + the broadcasting services. Called automatically
    /// by <c>AddSimpleModuleInfrastructure</c>; modules can layer additional
    /// authorizers on top via <see cref="AddBroadcastAuthorizer{T}"/>.
    /// </summary>
    public static IServiceCollection AddSimpleModuleBroadcasting(this IServiceCollection services)
    {
        services.AddSignalR();
        services.TryAddSingleton<PresenceTracker>();
        services.TryAddSingleton<BroadcastAuthorizerChain>();
        services.TryAddSingleton<IBroadcaster, Broadcaster>();

        services.AddSingleton<IBroadcastChannelAuthorizer, DefaultBroadcastAuthorizer>();
        services.AddSingleton<IBroadcastChannelAuthorizer, UserChannelAuthorizer>();
        services.AddSingleton<IBroadcastChannelAuthorizer, TenantChannelAuthorizer>();

        // Decorate Wolverine's IMessageBus so any IBroadcastEvent published
        // through the bus is mirrored to SignalR clients. Wolverine owns the
        // base IMessageBus registration; Scrutor lets us wrap it without
        // taking over the registration ourselves. Matches the AuditingMessageBus
        // pattern so framework consumers see a single, consistent extension point.
        services.Decorate<IMessageBus, BroadcastingMessageBus>();

        return services;
    }

    /// <summary>
    /// Adds an additional <see cref="IBroadcastChannelAuthorizer"/>. The
    /// longest matching <see cref="IBroadcastChannelAuthorizer.ChannelPrefix"/>
    /// wins, so module-specific rules naturally override broader framework
    /// defaults.
    /// </summary>
    public static IServiceCollection AddBroadcastAuthorizer<TAuthorizer>(
        this IServiceCollection services
    )
        where TAuthorizer : class, IBroadcastChannelAuthorizer
    {
        services.AddSingleton<IBroadcastChannelAuthorizer, TAuthorizer>();
        return services;
    }

    /// <summary>
    /// Maps the broadcast hub onto <see cref="BroadcastHub.Endpoint"/>.
    /// Called by <c>UseSimpleModuleInfrastructure</c>; the route inherits
    /// the framework's authentication / fallback authorization policy.
    /// </summary>
    public static IEndpointRouteBuilder MapSimpleModuleBroadcasting(
        this IEndpointRouteBuilder endpoints
    )
    {
        endpoints.MapHub<BroadcastHub>(BroadcastHub.Endpoint);
        return endpoints;
    }
}
