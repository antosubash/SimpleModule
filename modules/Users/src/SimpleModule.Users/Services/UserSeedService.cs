using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Services;

public partial class UserSeedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<UserSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<
                RoleManager<ApplicationRole>
            >();
            var userManager = scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>
            >();

            await SeedRoleAsync(
                roleManager,
                SeedConstants.AdminRole,
                SeedConstants.AdminRoleDescription
            );
            await SeedRoleAsync(
                roleManager,
                SeedConstants.UserRole,
                SeedConstants.UserRoleDescription
            );
            await SeedUserAsync(
                userManager,
                SeedConstants.AdminEmail,
                SeedConstants.AdminDisplayName,
                ConfigKeys.SeedAdminPassword,
                SeedConstants.DefaultAdminPassword,
                SeedConstants.AdminRole
            );
            await SeedUserAsync(
                userManager,
                SeedConstants.UserEmail,
                SeedConstants.UserDisplayName,
                ConfigKeys.SeedUserPassword,
                SeedConstants.DefaultUserPassword,
                SeedConstants.UserRole
            );
        }
#pragma warning disable CA1031 // Seed service must not crash the host on database errors
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogSeedError(logger, ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedRoleAsync(
        RoleManager<ApplicationRole> roleManager,
        string name,
        string description
    )
    {
        if (await roleManager.RoleExistsAsync(name))
            return;

        LogSeedingRole(logger, name);

        var result = await roleManager.CreateAsync(
            new ApplicationRole
            {
                Name = name,
                Description = description,
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

    private async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string passwordConfigKey,
        string defaultPassword,
        string role
    )
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        LogSeedingUser(logger, email);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
        };

        var password = configuration[passwordConfigKey] ?? defaultPassword;
        if (password == defaultPassword && !environment.IsDevelopment())
            LogDefaultPasswordWarning(logger, email, passwordConfigKey);
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            foreach (var error in result.Errors)
            {
                LogSeedError(logger, error.Description);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding role: {RoleName}")]
    private static partial void LogSeedingRole(ILogger logger, string roleName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding user: {Email}")]
    private static partial void LogSeedingUser(ILogger logger, string email);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Seeding {Email} with default password. Set '{ConfigKey}' in configuration before deploying to production."
    )]
    private static partial void LogDefaultPasswordWarning(
        ILogger logger,
        string email,
        string configKey
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Seed error: {ErrorDescription}")]
    private static partial void LogSeedError(ILogger logger, string errorDescription);
}
