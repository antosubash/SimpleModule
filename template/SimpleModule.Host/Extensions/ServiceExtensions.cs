using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using SimpleModule.Blazor;
using SimpleModule.Core;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Events;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Database.Health;
using SimpleModule.Host.Components;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.Host;

public static class ServiceExtensions
{
    /// <summary>
    /// Registers Swagger/OpenAPI with OAuth2 security scheme for OpenIddict.
    /// Configures Authorization Code flow for interactive API documentation testing.
    /// </summary>
    public static IServiceCollection AddSwaggerWithOpenIddict(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(
                AuthConstants.OAuth2Scheme,
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(
                                ConnectRouteConstants.ConnectAuthorize,
                                UriKind.Relative
                            ),
                            TokenUrl = new Uri(
                                ConnectRouteConstants.ConnectToken,
                                UriKind.Relative
                            ),
                            Scopes = new Dictionary<string, string>
                            {
                                { AuthConstants.OpenIdScope, "OpenID" },
                                { AuthConstants.ProfileScope, "Profile" },
                                { AuthConstants.EmailScope, "Email" },
                            },
                        },
                    },
                }
            );
            options.AddSecurityRequirement(doc =>
            {
                var scheme =
                    doc.Components?.SecuritySchemes?.ContainsKey(AuthConstants.OAuth2Scheme) == true
                        ? new OpenApiSecuritySchemeReference(AuthConstants.OAuth2Scheme, doc)
                        : null;
                if (scheme is null)
                    return new OpenApiSecurityRequirement();
                return new OpenApiSecurityRequirement
                {
                    {
                        scheme,
                        [
                            AuthConstants.OpenIdScope,
                            AuthConstants.ProfileScope,
                            AuthConstants.EmailScope,
                        ]
                    },
                };
            });
        });

        return services;
    }

    /// <summary>
    /// Registers Blazor SSR, Inertia page renderer, event bus, and shared data context.
    /// </summary>
    public static IServiceCollection AddInertiaServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddRazorComponents();

        services.AddSimpleModuleBlazor(options =>
        {
            options.ShellComponent = typeof(InertiaShell);
        });

        services.AddScoped<IEventBus, EventBus>();
        services.AddScoped<InertiaSharedData>();

        return services;
    }

    /// <summary>
    /// Registers all modules, module database context, menu items, settings, and page registry.
    /// </summary>
    public static IServiceCollection AddModuleSystem(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddModules(configuration);

        services.AddModuleDbContext<HostDbContext>(
            configuration,
            "Host",
            opts => opts.UseOpenIddict()
        );

        services.CollectModuleMenuItems();
        services.CollectModuleSettings();
        services.AddSingleton<IReadOnlyList<AvailablePage>>(PageRegistry.Pages);

        return services;
    }

    /// <summary>
    /// Registers a policy-based authentication scheme that automatically selects between:
    /// - OpenIddict Bearer token validation (if Authorization header contains "Bearer ...")
    /// - ASP.NET Identity cookie authentication (otherwise)
    /// </summary>
    public static IServiceCollection AddSmartAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication()
            .AddPolicyScheme(
                AuthConstants.SmartAuthPolicy,
                null!,
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                        if (
                            authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            == true
                        )
                            return OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                        return IdentityConstants.ApplicationScheme;
                    };
                }
            );

        services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = AuthConstants.SmartAuthPolicy;
            options.DefaultAuthenticateScheme = AuthConstants.SmartAuthPolicy;
            options.DefaultChallengeScheme = AuthConstants.SmartAuthPolicy;
        });

        return services;
    }

    /// <summary>
    /// Registers health checks with database readiness probe.
    /// </summary>
    public static IServiceCollection AddModuleHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                HealthCheckConstants.DatabaseCheckName,
                tags: [HealthCheckConstants.ReadyTag]
            );

        return services;
    }
}
