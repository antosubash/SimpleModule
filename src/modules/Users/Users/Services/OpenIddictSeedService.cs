using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using SimpleModule.Users.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.Users.Services;

public partial class OpenIddictSeedService(
    IServiceProvider serviceProvider,
    ILogger<OpenIddictSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        await SeedClientApplicationAsync(scope, cancellationToken);
        await SeedRolesAsync(scope);
        await SeedAdminUserAsync(scope);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedClientApplicationAsync(
        IServiceScope scope,
        CancellationToken cancellationToken
    )
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync("simplemodule-client", cancellationToken) is not null)
        {
            return;
        }

        LogSeedingClient(logger);

        await manager.CreateAsync(
            new OpenIddictApplicationDescriptor
            {
                ClientId = "simplemodule-client",
                DisplayName = "SimpleModule Client",
                ClientType = ClientTypes.Public,
                RedirectUris =
                {
                    new Uri("https://localhost:5001/swagger/oauth2-redirect.html"),
                    new Uri("https://localhost:5001/oauth-callback"),
                },
                PostLogoutRedirectUris = { new Uri("https://localhost:5001/") },
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
                    Permissions.Prefixes.Scope + "roles",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            },
            cancellationToken
        );
    }

    private async Task SeedRolesAsync(IServiceScope scope)
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (await roleManager.RoleExistsAsync("Admin"))
        {
            return;
        }

        LogSeedingRoles(logger);

        var result = await roleManager.CreateAsync(
            new ApplicationRole
            {
                Name = "Admin",
                Description = "Administrator role with full access",
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

        if (await userManager.FindByEmailAsync("admin@simplemodule.dev") is not null)
        {
            return;
        }

        LogSeedingAdmin(logger);

        var admin = new ApplicationUser
        {
            UserName = "admin@simplemodule.dev",
            Email = "admin@simplemodule.dev",
            DisplayName = "Admin",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(admin, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
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
