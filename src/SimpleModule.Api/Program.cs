using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using SimpleModule.Core;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Database;
using SimpleModule.Database.Health;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "oauth2",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri("/connect/authorize", UriKind.Relative),
                    TokenUrl = new Uri("/connect/token", UriKind.Relative),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID" },
                        { "profile", "Profile" },
                        { "email", "Email" },
                    },
                },
            },
        }
    );
    options.AddSecurityRequirement(doc =>
    {
        var scheme = doc.Components?.SecuritySchemes?.ContainsKey("oauth2") == true
            ? new OpenApiSecuritySchemeReference("oauth2", doc)
            : null;
        if (scheme is null)
            return new OpenApiSecurityRequirement();
        return new OpenApiSecurityRequirement { { scheme, ["openid", "profile", "email"] } };
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Razor Pages for Identity UI
builder.Services.AddRazorPages();

// Register event bus
builder.Services.AddScoped<IEventBus, EventBus>();

// Register all modules
builder.Services.AddModules(builder.Configuration);

// Override default auth scheme — OpenIddict validation for Bearer tokens
builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});
builder.Services.AddAuthorization();

// Health checks
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

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
        options.OAuthClientId("simplemodule-client");
        options.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Health endpoints
app.MapHealthChecks("/health");

// Identity UI pages
app.MapRazorPages();

// Automatically map all module endpoints
app.MapModuleEndpoints();

app.Run();
