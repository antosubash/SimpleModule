using Microsoft.AspNetCore.Identity;
using SimpleModule.AuditLogs.Middleware;
using SimpleModule.Blazor;
using SimpleModule.Core;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Exceptions;
using SimpleModule.Core.Inertia;
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

// Validate database configuration early
try
{
    var dbOptions =
        builder.Configuration.GetSection("Database").Get<SimpleModule.Database.DatabaseOptions>()
        ?? new SimpleModule.Database.DatabaseOptions();
    var connString = dbOptions.DefaultConnection;

    if (string.IsNullOrEmpty(connString))
    {
        throw new InvalidOperationException(
            "Database configuration is missing. "
                + "Ensure 'Database:DefaultConnection' is configured in appsettings.json."
        );
    }

    // Validate the provider configuration
    _ = SimpleModule.Database.DatabaseProviderDetector.Detect(connString, dbOptions.Provider);
}
catch (InvalidOperationException ex)
{
    // CA1849: WriteLine is OK during startup before async context
#pragma warning disable CA1849
    Console.Error.WriteLine($"FATAL: Database configuration error - {ex.Message}");
#pragma warning restore CA1849
    throw;
}

// Register services
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSwaggerWithOpenIddict();
builder.Services.AddInertiaServices(builder.Configuration);
builder.Services.AddModuleSystem(builder.Configuration);
builder.Services.AddSmartAuthentication();
builder.Services.AddModuleHealthChecks();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<SimpleModule.Host.Services.ViteDevWatchService>();
}

var app = builder.Build();

// Initialize database
await app.InitializeDatabaseAsync();

// Configure middleware pipeline
app.UseExceptionHandler();

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
app.UseStaticFileCaching();
app.MapStaticAssets();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();
app.UseHomePageRewrite();
app.UseAntiforgery();
app.MapModuleHealthChecks();
app.MapModuleComponents();
app.MapModuleEndpoints();
app.MapDefaultEndpoints();

await app.RunAsync();
