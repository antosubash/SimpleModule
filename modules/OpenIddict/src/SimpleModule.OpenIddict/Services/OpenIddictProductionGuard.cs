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
/// in a single request. Missing token signing/encryption certificates only log
/// a prominent warning: the app starts on ephemeral keys so a plain
/// <c>docker run</c> of the image works out of the box, but those keys are
/// regenerated on every restart, invalidating all issued tokens and signing
/// everyone out on each redeploy. Configure real certificates for anything
/// beyond a throwaway deployment. Shares
/// <see cref="HostEnvironmentExtensions.IsLocalOrTest"/> with
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

        if (string.IsNullOrEmpty(encryptionCertPath) || string.IsNullOrEmpty(signingCertPath))
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
