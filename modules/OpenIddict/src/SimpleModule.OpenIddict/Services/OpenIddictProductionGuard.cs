using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Services;

/// <summary>
/// Fails host startup when the OpenIddict configuration is unsafe for Production:
/// the ROPC password grant must stay off (it lets anyone exchange leaked or default
/// credentials for a fully-privileged token in a single request), and token
/// signing/encryption must use real certificates — ephemeral keys are regenerated
/// on every restart, invalidating all issued tokens, and signal a copy-pasted
/// Development configuration.
/// </summary>
public sealed class OpenIddictProductionGuard(
    IConfiguration configuration,
    IHostEnvironment environment
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsProduction())
        {
            return Task.CompletedTask;
        }

        if (configuration.GetValue<bool>("OpenIddict:AllowPasswordGrant"))
        {
            throw new InvalidOperationException(
                "'OpenIddict:AllowPasswordGrant' must not be enabled in Production. "
                    + "The ROPC password grant exists for local load testing only."
            );
        }

        var encryptionCertPath = configuration[ConfigKeys.OpenIddictEncryptionCertPath];
        var signingCertPath = configuration[ConfigKeys.OpenIddictSigningCertPath];

        if (string.IsNullOrEmpty(encryptionCertPath) || string.IsNullOrEmpty(signingCertPath))
        {
            throw new InvalidOperationException(
                $"'{ConfigKeys.OpenIddictSigningCertPath}' and "
                    + $"'{ConfigKeys.OpenIddictEncryptionCertPath}' must be configured in "
                    + "Production. Without certificates OpenIddict falls back to ephemeral "
                    + "keys that are regenerated on every restart, invalidating all issued "
                    + "tokens."
            );
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
