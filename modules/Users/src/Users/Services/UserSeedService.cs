using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Services;

public partial class UserSeedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<UserSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment())
            return;

        try
        {
            using var scope = serviceProvider.CreateScope();
            await SeedRolesAsync(scope);
            await SeedAdminUserAsync(scope);
        }
#pragma warning disable CA1031 // Seed service must not crash the host on database errors
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogSeedError(logger, ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
