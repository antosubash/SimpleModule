using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using SimpleModule.Core.Constants;
using SimpleModule.Users.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.Users.Services;

public partial class OpenIddictSeedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<OpenIddictSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        await SeedClientApplicationAsync(scope, cancellationToken);

        // Only seed default roles and admin user in Development
        if (hostEnvironment.IsDevelopment())
        {
            await SeedRolesAsync(scope);
            await SeedAdminUserAsync(scope);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedClientApplicationAsync(
        IServiceScope scope,
        CancellationToken cancellationToken
    )
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (
            await manager.FindByClientIdAsync(ClientConstants.ClientId, cancellationToken)
            is not null
        )
        {
            return;
        }

        LogSeedingClient(logger);

        var baseUrl = configuration[ConfigKeys.OpenIddictBaseUrl] ?? ClientConstants.DefaultBaseUrl;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = ClientConstants.ClientId,
            DisplayName = ClientConstants.ClientDisplayName,
            ClientType = ClientTypes.Public,
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
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + AuthConstants.RolesScope,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        };

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

        await manager.CreateAsync(descriptor, cancellationToken);
    }

    private async Task SeedRolesAsync(IServiceScope scope)
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (await roleManager.RoleExistsAsync(SeedConstants.AdminRole))
        {
            return;
        }

        LogSeedingRoles(logger);

        var result = await roleManager.CreateAsync(
            new ApplicationRole
            {
                Name = SeedConstants.AdminRole,
                Description = SeedConstants.AdminRoleDescription,
                CreatedAt = DateTime.UtcNow,
            }
        );

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                LogSeedError(logger, error.Description);
            }
        }
    }

    private async Task SeedAdminUserAsync(IServiceScope scope)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(SeedConstants.AdminEmail) is not null)
        {
            return;
        }

        LogSeedingAdmin(logger);

        var admin = new ApplicationUser
        {
            UserName = SeedConstants.AdminEmail,
            Email = SeedConstants.AdminEmail,
            DisplayName = SeedConstants.AdminDisplayName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
        };

        var adminPassword =
            configuration[ConfigKeys.SeedAdminPassword] ?? SeedConstants.DefaultAdminPassword;
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, SeedConstants.AdminRole);
        }
        else
        {
            foreach (var error in result.Errors)
            {
                LogSeedError(logger, error.Description);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeding OpenIddict client application..."
    )]
    private static partial void LogSeedingClient(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding roles...")]
    private static partial void LogSeedingRoles(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding admin user...")]
    private static partial void LogSeedingAdmin(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error seeding admin user: {ErrorDescription}"
    )]
    private static partial void LogSeedError(ILogger logger, string errorDescription);
}
