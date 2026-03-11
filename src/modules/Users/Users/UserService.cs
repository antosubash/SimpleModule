using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users;

public partial class UserService(
    UserManager<ApplicationUser> userManager,
    ILogger<UserService> logger
) : IUserContracts
{
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();
        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            LogUserNotFound(logger, id);
            return null;
        }

        return MapToDto(user);
    }

    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
        return await GetUserByIdAsync(userId);
    }

    private static UserDto MapToDto(ApplicationUser user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
        };

    [LoggerMessage(Level = LogLevel.Warning, Message = "User with ID {UserId} not found")]
    private static partial void LogUserNotFound(ILogger logger, string userId);
}
