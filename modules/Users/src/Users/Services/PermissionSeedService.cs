using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Authorization;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Services;

public partial class PermissionSeedService(
    IServiceProvider serviceProvider,
    PermissionRegistry permissionRegistry,
    ILogger<PermissionSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        var adminRole = await roleManager.FindByNameAsync(SeedConstants.AdminRole);
        if (adminRole is null)
            return;

        var existingPermissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingPermissions);
        var newPermissions = new List<RolePermission>();

        foreach (var permission in permissionRegistry.AllPermissions)
        {
            if (!existingSet.Contains(permission))
            {
                newPermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    Permission = permission,
                });
            }
        }

        if (newPermissions.Count > 0)
        {
            LogSeedingPermissions(logger, newPermissions.Count);
            dbContext.RolePermissions.AddRange(newPermissions);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeding {Count} permissions for Admin role..."
    )]
    private static partial void LogSeedingPermissions(ILogger logger, int count);
}
