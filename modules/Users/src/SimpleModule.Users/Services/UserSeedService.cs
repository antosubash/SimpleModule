using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Hosting;
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
                SeedConstants.AdminRole,
                requiredOutsideDevelopment: true
            );
            await SeedUserAsync(
                userManager,
                SeedConstants.UserEmail,
                SeedConstants.UserDisplayName,
                ConfigKeys.SeedUserPassword,
                SeedConstants.DefaultUserPassword,
                SeedConstants.UserRole,
                requiredOutsideDevelopment: false
            );
        }
#pragma warning disable CA1031 // Seed service must not crash the host on database errors
        catch (Exception ex) when (ex is not SeedConfigurationException)
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
        string role,
        bool requiredOutsideDevelopment
    )
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        // The compiled-in default passwords are a local/CI convenience only.
        // In a real deployment (anything but Development/Testing) the configured
        // password is mandatory: creating the admin account with a published
        // default would leave it one POST /connect/token away from a
        // fully-privileged token. Fail host startup for the required (admin)
        // account; simply skip the optional demo user.
        var password = configuration[passwordConfigKey];
        if (string.IsNullOrEmpty(password))
        {
            if (environment.IsLocalOrTest())
            {
                password = defaultPassword;
            }
            else if (requiredOutsideDevelopment)
            {
                throw new SeedConfigurationException(
                    $"'{passwordConfigKey}' must be configured in the '{environment.EnvironmentName}' "
                        + $"environment. Refusing to create '{email}' with the compiled-in default "
                        + "password."
                );
            }
            else
            {
                LogSkippingSeedUser(logger, email, passwordConfigKey);
                return;
            }
        }

        LogSeedingUser(logger, email);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
        };

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
        Level = LogLevel.Information,
        Message = "Skipping seed user {Email}: '{ConfigKey}' is not configured and default passwords are disabled outside Development."
    )]
    private static partial void LogSkippingSeedUser(ILogger logger, string email, string configKey);

    [LoggerMessage(Level = LogLevel.Error, Message = "Seed error: {ErrorDescription}")]
    private static partial void LogSeedError(ILogger logger, string errorDescription);
}
