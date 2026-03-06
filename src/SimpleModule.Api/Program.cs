using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using SimpleModule.Api.Components;
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
        var scheme =
            doc.Components?.SecuritySchemes?.ContainsKey("oauth2") == true
                ? new OpenApiSecuritySchemeReference("oauth2", doc)
                : null;
        if (scheme is null)
            return new OpenApiSecurityRequirement();
        return new OpenApiSecurityRequirement { { scheme, ["openid", "profile", "email"] } };
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
    .AddPolicyScheme("SmartAuth", null!, options =>
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
    options.DefaultScheme = "SmartAuth";
    options.DefaultAuthenticateScheme = "SmartAuth";
    options.DefaultChallengeScheme = "SmartAuth";
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
app.UseAntiforgery();

// Health endpoints
app.MapHealthChecks("/health");

// Blazor SSR
app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(SimpleModule.Users.UsersModule).Assembly);

// Automatically map all module endpoints
app.MapModuleEndpoints();

app.Run();
