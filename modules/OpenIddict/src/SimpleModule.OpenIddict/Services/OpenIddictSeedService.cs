using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Services;

public partial class OpenIddictSeedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<OpenIddictSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            await SeedClientApplicationAsync(scope, cancellationToken);
        }
#pragma warning disable CA1031 // Seed service must not crash the host on database errors
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogSeedError(logger, ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedClientApplicationAsync(
        IServiceScope scope,
        CancellationToken cancellationToken
    )
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var baseUrl = configuration[ConfigKeys.OpenIddictBaseUrl] ?? ClientConstants.DefaultBaseUrl;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = ClientConstants.ClientId,
            DisplayName = ClientConstants.ClientDisplayName,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            RedirectUris =
            {
                new Uri($"{baseUrl}{ClientConstants.SwaggerCallbackPath}"),
                new Uri($"{baseUrl}{ClientConstants.OAuthCallbackPath}"),
            },
            PostLogoutRedirectUris =
            {
                new Uri($"{baseUrl}{ClientConstants.PostLogoutRedirectPath}"),
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + AuthConstants.RolesScope,
            },
            Requirements = { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange },
        };

        // Allow password grant in Development for load testing (k6, etc.)
        if (configuration.GetValue<bool>("OpenIddict:AllowPasswordGrant"))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
        }

        // Allow additional redirect URIs from configuration
        var additionalRedirects = configuration
            .GetSection(ConfigKeys.OpenIddictAdditionalRedirectUris)
            .Get<string[]>();
        if (additionalRedirects is not null)
        {
            foreach (var uri in additionalRedirects)
            {
                descriptor.RedirectUris.Add(new Uri(uri));
            }
        }

        var existing = await manager.FindByClientIdAsync(
            ClientConstants.ClientId,
            cancellationToken
        );
        if (existing is not null)
        {
            LogUpdatingClient(logger);
            await manager.UpdateAsync(existing, descriptor, cancellationToken);
            return;
        }

        LogSeedingClient(logger);
        await manager.CreateAsync(descriptor, cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeding OpenIddict client application..."
    )]
    private static partial void LogSeedingClient(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Updating OpenIddict client application redirect URIs..."
    )]
    private static partial void LogUpdatingClient(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "OpenIddict seeding skipped due to error: {ErrorMessage}"
    )]
    private static partial void LogSeedError(ILogger logger, string errorMessage);
}
