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

        var configuredPassword = configuration[passwordConfigKey];
        var password = ResolveSeedPassword(configuredPassword, defaultPassword);

        if (string.IsNullOrEmpty(configuredPassword) && !environment.IsLocalOrTest())
        {
            LogDefaultPasswordWarning(logger, email, passwordConfigKey);
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

    /// <summary>
    /// Resolves the password to seed an account with: the configured value when one
    /// is present, otherwise the compiled-in default. Extracted as a pure function
    /// so the fallback is unit-testable without an Identity stack.
    /// </summary>
    /// <remarks>
    /// Both seeded accounts always fall back to their compiled-in defaults so the
    /// app boots and is usable out of the box in every environment — the login page
    /// advertises these same credentials via its quick-login buttons, so seeding
    /// them is what makes those buttons work. The defaults are published in this
    /// repository: any deployment that is reachable by anyone else must set the
    /// <c>Seed:AdminPassword</c> configuration key (or rotate the password after
    /// first login), and turn off the "Show Test Accounts" system setting
    /// (<c>auth.show_test_accounts</c>) from the Settings UI — that one is a
    /// <c>SettingDefinition</c> read through <c>ISettingsContracts</c>, not an
    /// <c>IConfiguration</c> key, so it cannot be set via an environment variable.
    /// </remarks>
    /// <param name="configuredPassword">The password from configuration, if any.</param>
    /// <param name="defaultPassword">The compiled-in fallback.</param>
    internal static string ResolveSeedPassword(
        string? configuredPassword,
        string defaultPassword
    ) => string.IsNullOrEmpty(configuredPassword) ? defaultPassword : configuredPassword;

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding role: {RoleName}")]
    private static partial void LogSeedingRole(ILogger logger, string roleName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding user: {Email}")]
    private static partial void LogSeedingUser(ILogger logger, string email);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Seeded {Email} with the compiled-in default password because '{ConfigKey}' is not configured. This password is published in the SimpleModule repository — set '{ConfigKey}' or change the password after first login before exposing this deployment."
    )]
    private static partial void LogDefaultPasswordWarning(
        ILogger logger,
        string email,
        string configKey
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Seed error: {ErrorDescription}")]
    private static partial void LogSeedError(ILogger logger, string errorDescription);
}
