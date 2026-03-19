using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using SimpleModule.Blazor;
using SimpleModule.Core;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Core.Inertia;
using SimpleModule.Database;
using SimpleModule.Database.Health;
using SimpleModule.Host;
using SimpleModule.Host.Components;
using SimpleModule.OpenIddict.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (OpenTelemetry, resilience, service discovery)
builder.AddServiceDefaults();

// Bridge Aspire-managed connection string to Database options
var aspireConnectionString = builder.Configuration.GetConnectionString("simplemoduledb");
if (!string.IsNullOrEmpty(aspireConnectionString))
{
    builder.Configuration["Database:DefaultConnection"] = aspireConnectionString;
}

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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
                    TokenUrl = new Uri(ConnectRouteConstants.ConnectToken, UriKind.Relative),
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
                [AuthConstants.OpenIdScope, AuthConstants.ProfileScope, AuthConstants.EmailScope]
            },
        };
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Blazor SSR
builder.Services.AddRazorComponents();

// Inertia page renderer (renders Inertia HTML shell via Blazor SSR)
builder.Services.AddSimpleModuleBlazor(options =>
{
    options.ShellComponent = typeof(InertiaShell);
});

// Register event bus
builder.Services.AddScoped<IEventBus, EventBus>();

// Inertia shared data (per-request bag for props shared across all Inertia responses)
builder.Services.AddScoped<InertiaSharedData>();

// Register all modules
builder.Services.AddModules(builder.Configuration);

// Register unified HostDbContext for EF Core migrations
builder.Services.AddModuleDbContext<HostDbContext>(
    builder.Configuration,
    "Host",
    opts => opts.UseOpenIddict()
);

// Collect module menu items into a singleton registry
builder.Services.CollectModuleMenuItems();

// Collect module settings definitions into a singleton registry
builder.Services.CollectModuleSettings();

// Smart auth: Bearer header → OpenIddict validation; otherwise → Identity cookies
builder
    .Services.AddAuthentication()
    .AddPolicyScheme(
        AuthConstants.SmartAuthPolicy,
        null!,
        options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                    return OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                return IdentityConstants.ApplicationScheme;
            };
        }
    );
builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultScheme = AuthConstants.SmartAuthPolicy;
    options.DefaultAuthenticateScheme = AuthConstants.SmartAuthPolicy;
    options.DefaultChallengeScheme = AuthConstants.SmartAuthPolicy;
});

// Health checks
builder
    .Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        HealthCheckConstants.DatabaseCheckName,
        tags: [HealthCheckConstants.ReadyTag]
    );

var app = builder.Build();

// Apply database schema (production should use explicit migration tooling)
if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HostDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(ClientConstants.ClientId);
        options.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();

app.UseInertia();

app.Use(
    async (context, next) =>
    {
        var path = context.Request.Path.Value;
        bool hasVersionParam = context.Request.Query.ContainsKey("v");
        bool isVendorJs =
            path is not null && path.StartsWith("/js/vendor/", StringComparison.OrdinalIgnoreCase);
        bool isHashedChunk =
            path is not null
            && path.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase);

        if (hasVersionParam || isVendorJs || isHashedChunk)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                return Task.CompletedTask;
            });
        }

        await next();
    }
);

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Health endpoints — liveness (no checks) and readiness (database checks)
app.MapHealthChecks(
        RouteConstants.HealthLive,
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false, // No checks — just confirms the process is running
        }
    )
    .AllowAnonymous();
app.MapHealthChecks(
        RouteConstants.HealthReady,
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckConstants.ReadyTag),
        }
    )
    .AllowAnonymous();

// Blazor SSR
app.MapRazorComponents<App>().AddModuleAssemblies();

// Automatically map all module endpoints
app.MapModuleEndpoints();

// Aspire default health endpoints (/health, /alive)
app.MapDefaultEndpoints();

await app.RunAsync();
