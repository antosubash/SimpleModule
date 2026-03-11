using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using SimpleModule.Api.Components;
using SimpleModule.Core;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Database;
using SimpleModule.Core.Constants;
using SimpleModule.Database.Health;

var builder = WebApplication.CreateBuilder(args);

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
                    AuthorizationUrl = new Uri(RouteConstants.ConnectAuthorize, UriKind.Relative),
                    TokenUrl = new Uri(RouteConstants.ConnectToken, UriKind.Relative),
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
        return new OpenApiSecurityRequirement { { scheme, [AuthConstants.OpenIdScope, AuthConstants.ProfileScope, AuthConstants.EmailScope] } };
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Blazor SSR
builder.Services.AddRazorComponents();

// Register event bus
builder.Services.AddScoped<IEventBus, EventBus>();

// Register all modules
builder.Services.AddModules(builder.Configuration);

// Smart auth: Bearer header → OpenIddict validation; otherwise → Identity cookies
builder.Services.AddAuthentication()
    .AddPolicyScheme(AuthConstants.SmartAuthPolicy, null!, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                return OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            return IdentityConstants.ApplicationScheme;
        };
    });
builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultScheme = AuthConstants.SmartAuthPolicy;
    options.DefaultAuthenticateScheme = AuthConstants.SmartAuthPolicy;
    options.DefaultChallengeScheme = AuthConstants.SmartAuthPolicy;
});
builder.Services.AddAuthorization();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(HealthCheckConstants.DatabaseCheckName, tags: [HealthCheckConstants.ReadyTag]);

var app = builder.Build();

// Ensure databases are created with seed data
app.EnsureModuleDatabases();

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

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Health endpoints — liveness (no checks) and readiness (database checks)
app.MapHealthChecks(RouteConstants.HealthLive, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false, // No checks — just confirms the process is running
});
app.MapHealthChecks(RouteConstants.HealthReady, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckConstants.ReadyTag),
});

// Blazor SSR
app.MapRazorComponents<App>()
    .AddModuleAssemblies();

// Automatically map all module endpoints
app.MapModuleEndpoints();

app.Run();
