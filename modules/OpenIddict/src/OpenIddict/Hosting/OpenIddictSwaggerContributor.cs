using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using SimpleModule.OpenIddict.Contracts;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SimpleModule.OpenIddict.Hosting;

[SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Instantiated via DI")]
internal sealed class OpenIddictSwaggerGenSetup : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
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
                    [
                        AuthConstants.OpenIdScope,
                        AuthConstants.ProfileScope,
                        AuthConstants.EmailScope,
                    ]
                },
            };
        });
    }
}

[SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Instantiated via DI")]
internal sealed class OpenIddictSwaggerUISetup : IConfigureOptions<SwaggerUIOptions>
{
    public void Configure(SwaggerUIOptions options)
    {
        options.OAuthClientId(ClientConstants.ClientId);
        options.OAuthUsePkce();
    }
}
