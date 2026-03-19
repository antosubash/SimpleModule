using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Authorization;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Entities;
using RolePermission = SimpleModule.Permissions.Entities.RolePermission;

namespace SimpleModule.Permissions.Services;

public partial class PermissionSeedService(
    IServiceProvider serviceProvider,
    PermissionRegistry permissionRegistry,
    ILogger<PermissionSeedService> logger
) : IHostedService
{
    private const string AdminRole = "Admin";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PermissionsDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<
                RoleManager<ApplicationRole>
            >();

            var adminRole = await roleManager.FindByNameAsync(AdminRole);
            if (adminRole is null)
            {
                return;
            }

            var adminRoleId = RoleId.From(adminRole.Id);

            var existingPermissions = await dbContext
                .RolePermissions.Where(rp => rp.RoleId == adminRoleId)
                .Select(rp => rp.Permission)
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<string>(existingPermissions);
            var newPermissions = new List<RolePermission>();

            foreach (var permission in permissionRegistry.AllPermissions)
            {
                if (!existingSet.Contains(permission))
                {
                    newPermissions.Add(
                        new RolePermission { RoleId = adminRoleId, Permission = permission }
                    );
                }
            }

            if (newPermissions.Count > 0)
            {
                LogSeedingPermissions(logger, newPermissions.Count);
                dbContext.RolePermissions.AddRange(newPermissions);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
#pragma warning disable CA1031 // Seed service must not crash the host on database errors
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogSeedError(logger, ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeding {Count} permissions for Admin role..."
    )]
    private static partial void LogSeedingPermissions(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Permission seeding skipped due to error: {ErrorMessage}"
    )]
    private static partial void LogSeedError(ILogger logger, string errorMessage);
}
