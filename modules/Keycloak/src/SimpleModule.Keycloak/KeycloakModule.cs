using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SimpleModule.Core;
using SimpleModule.Identity.Contracts;
using SimpleModule.Keycloak.Contracts;
using SimpleModule.Keycloak.Services;

namespace SimpleModule.Keycloak;

[Module(KeycloakModuleConstants.ModuleName)]
public class KeycloakModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Identity:Provider");
        if (!string.Equals(provider, "Keycloak", StringComparison.OrdinalIgnoreCase))
            return;

        // Bind options
        var keycloakSection = configuration.GetSection(KeycloakOptions.SectionName);
        services.Configure<KeycloakOptions>(keycloakSection);
        var keycloakOptions = keycloakSection.Get<KeycloakOptions>() ?? new KeycloakOptions();

        // Identity provider metadata
        services.AddSingleton<IIdentityProvider, KeycloakIdentityProvider>();

        // Session management
        services.AddScoped<KeycloakSessionService>();
        services.AddScoped<ISessionContracts>(sp =>
            sp.GetRequiredService<KeycloakSessionService>()
        );

        // JIT user sync
        services.AddScoped<KeycloakUserSyncService>();

        // Claims transformation: maps Keycloak JWT claims to standard .NET claims.
        // Runs before PermissionClaimsTransformation (which resolves permissions from roles).
        services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformation>();

        // Singleton token cache for the Keycloak Admin REST API
        services.AddSingleton<KeycloakTokenCache>();

        // Typed HttpClient for Keycloak Admin REST API
        services.AddHttpClient<KeycloakAdminClient>();

        // Authentication: JwtBearer for API calls, OIDC for Inertia pages
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityAuthConstants.SmartAuthPolicy;
                options.DefaultAuthenticateScheme = IdentityAuthConstants.SmartAuthPolicy;
                options.DefaultChallengeScheme = IdentityAuthConstants.SmartAuthPolicy;
            })
            .AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.LoginPath = KeycloakModuleConstants.Routes.Login;
                    options.LogoutPath = KeycloakModuleConstants.Routes.Logout;
                }
            )
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = keycloakOptions.Authority;
                    options.Audience = keycloakOptions.ClientId;
                    options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;

                    // Preserve original claim types — prevent the JWT handler from
                    // mapping "sub" -> ClaimTypes.NameIdentifier etc. Keycloak uses
                    // non-standard claim structures (realm_access) which
                    // KeycloakClaimsTransformation handles explicitly.
                    options.MapInboundClaims = false;
                }
            )
            .AddOpenIdConnect(
                KeycloakModuleConstants.OidcSchemeName,
                options =>
                {
                    options.Authority = keycloakOptions.Authority;
                    options.ClientId = keycloakOptions.ClientId;
                    options.ClientSecret = keycloakOptions.ClientSecret;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.MapInboundClaims = false;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("roles");

                    options.CallbackPath = KeycloakModuleConstants.Routes.Callback;

                    // Map Keycloak claims to well-known types
                    options.TokenValidationParameters.NameClaimType = "preferred_username";
                    options.TokenValidationParameters.RoleClaimType = "roles";
                }
            )
            .AddPolicyScheme(
                IdentityAuthConstants.SmartAuthPolicy,
                "Smart Authentication",
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                        if (
                            authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            == true
                        )
                            return JwtBearerDefaults.AuthenticationScheme;

                        // Cookie-based Inertia/browser requests
                        return CookieAuthenticationDefaults.AuthenticationScheme;
                    };
                }
            );
    }
}
