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

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new Core.Exceptions.ValidationException(errors);
        }

        LogUserCreated(logger, user.Id, user.Email);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            throw new Core.Exceptions.NotFoundException("User", id);
        }

        user.Email = request.Email;
        user.UserName = request.Email;
        user.DisplayName = request.DisplayName;

        await userManager.UpdateAsync(user);

        LogUserUpdated(logger, user.Id);

        return MapToDto(user);
    }

    public async Task DeleteUserAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            throw new Core.Exceptions.NotFoundException("User", id);
        }

        await userManager.DeleteAsync(user);

        LogUserDeleted(logger, id);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "User with ID {UserId} not found")]
    private static partial void LogUserNotFound(ILogger logger, string userId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "User {UserId} created with email {Email}"
    )]
    private static partial void LogUserCreated(ILogger logger, string userId, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} updated")]
    private static partial void LogUserUpdated(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} deleted")]
    private static partial void LogUserDeleted(ILogger logger, string userId);
}
