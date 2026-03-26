using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Hosting;

/// <summary>
/// Registers the SmartAuth policy scheme that selects between Bearer token
/// and cookie authentication. Called directly from OpenIddictModule.ConfigureServices.
/// </summary>
internal static class OpenIddictAuthSetup
{
    public static void AddSmartAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication()
            .AddPolicyScheme(
                AuthConstants.SmartAuthPolicy,
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
    }
}
