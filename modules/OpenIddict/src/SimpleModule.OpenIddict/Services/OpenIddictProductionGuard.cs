using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Hosting;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Services;

/// <summary>
/// Guards the OpenIddict configuration in real deployments (anything but
/// Development/Testing). The ROPC password grant fails host startup — it lets
/// anyone exchange leaked or default credentials for a fully-privileged token
/// in a single request. A half-configured certificate pair (exactly one of the
/// signing/encryption paths set) also fails startup: the module only uses real
/// certificates when both are present, so the configured one would be silently
/// ignored — that is always an operator mistake, not a deliberate choice.
/// Only the fully-unconfigured case logs a prominent warning instead: the app
/// starts on ephemeral keys so a plain <c>docker run</c> of the image works
/// out of the box, but those keys are regenerated on every restart,
/// invalidating all issued tokens and signing everyone out on each redeploy.
/// Configure real certificates for anything beyond a throwaway deployment.
/// Shares <see cref="HostEnvironmentExtensions.IsLocalOrTest"/> with
/// <c>UserSeedService</c> so the two guards never disagree about whether an
/// environment is a real deployment.
/// </summary>
public sealed partial class OpenIddictProductionGuard(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<OpenIddictProductionGuard> logger
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsLocalOrTest())
        {
            return Task.CompletedTask;
        }

        if (configuration.GetValue<bool>(ConfigKeys.OpenIddictAllowPasswordGrant))
        {
            throw new InvalidOperationException(
                $"'{ConfigKeys.OpenIddictAllowPasswordGrant}' must not be enabled in the "
                    + $"'{environment.EnvironmentName}' environment. The ROPC password grant "
                    + "exists for local load testing only."
            );
        }

        var encryptionCertPath = configuration[ConfigKeys.OpenIddictEncryptionCertPath];
        var signingCertPath = configuration[ConfigKeys.OpenIddictSigningCertPath];
        var hasEncryptionCert = !string.IsNullOrEmpty(encryptionCertPath);
        var hasSigningCert = !string.IsNullOrEmpty(signingCertPath);

        if (hasEncryptionCert != hasSigningCert)
        {
            var (missingKey, presentKey) = hasSigningCert
                ? (ConfigKeys.OpenIddictEncryptionCertPath, ConfigKeys.OpenIddictSigningCertPath)
                : (ConfigKeys.OpenIddictSigningCertPath, ConfigKeys.OpenIddictEncryptionCertPath);

            throw new InvalidOperationException(
                $"'{missingKey}' is not configured but '{presentKey}' is. Both certificates "
                    + "are required together — with only one configured it would be ignored "
                    + "and both token keys would fall back to ephemeral. Configure both, or "
                    + "neither to explicitly run on ephemeral keys."
            );
        }

        if (!hasEncryptionCert)
        {
            LogEphemeralKeys(
                logger,
                environment.EnvironmentName,
                ConfigKeys.OpenIddictSigningCertPath,
                ConfigKeys.OpenIddictEncryptionCertPath
            );
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No OpenIddict certificates configured in the '{EnvironmentName}' "
            + "environment — falling back to ephemeral signing/encryption keys. These keys "
            + "are regenerated on every restart, which invalidates all issued tokens and "
            + "signs everyone out on each redeploy. Configure '{SigningCertKey}' and "
            + "'{EncryptionCertKey}' (PKCS#12) for stable keys."
    )]
    private static partial void LogEphemeralKeys(
        ILogger logger,
        string environmentName,
        string signingCertKey,
        string encryptionCertKey
    );
}
